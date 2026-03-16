using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using MiniCRUFT.Audio;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.Renderer;
using MiniCRUFT.UI;
using MiniCRUFT.World;
using Veldrid;
using Veldrid.Sdl2;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class GameApp : IDisposable
{
    private readonly GameConfig _config;
    private readonly AssetStore _assets;
    private readonly RenderDevice _renderDevice;
    private WorldSession _session;
    private WorldEditor _worldEditor;
    private IWorldChangeQueue _changeQueue;
    private ChunkSaveQueue _saveQueue;
    private Player _player;
    private Camera _camera;
    private readonly InputState _input;
    private readonly InputHandler _inputHandler;
    private readonly Inventory _inventory;
    private readonly HudState _hud;
    private readonly Localization _localization;
    private readonly DayNightCycle _dayNight;
    private readonly WeatherSystem _weather;
    private readonly SoundRegistry _soundRegistry;
    private readonly SoundSystem _soundSystem;
    private readonly List<BiomeMenuItem> _biomeMenuItems = new();
    private bool _showBiomeMenu;
    private int _biomeMenuIndex;
    private bool _pendingRegen;
    private BiomeId? _pendingRegenBiome;
    private bool _exitRequested;
    private float _timeSeconds;
    private bool _paused;
    private bool _showDebug;
    private float _fpsTimer;
    private int _fpsFrames;
    private float _fps;
    private float _autoSaveTimer;

    private bool _leftHandled;
    private bool _rightHandled;
    private int _seed;

    public GameApp(GameConfig config)
    {
        _config = config;
        _assets = new AssetStore(config.AssetsPath);
        _renderDevice = RenderDevice.Create("MiniCRUFT", config.WindowWidth, config.WindowHeight, config.VSync);

        _renderDevice.Window.CursorVisible = false;
        SetMouseMode(captured: true, relative: true);
        _renderDevice.Window.KeyDown += key =>
        {
            if (_showBiomeMenu)
            {
                switch (key.Key)
                {
                    case Key.Up:
                    case Key.Left:
                        StepBiomeMenu(-1);
                        break;
                    case Key.Down:
                    case Key.Right:
                        StepBiomeMenu(1);
                        break;
                    case Key.Enter:
                        QueueWorldRegeneration();
                        break;
                    case Key.Escape:
                    case Key.F5:
                        _showBiomeMenu = false;
                        break;
                }
                return;
            }

            if (key.Key == Key.Escape)
            {
                _exitRequested = true;
            }
            else if (key.Key == Key.Tab)
            {
                TogglePause();
            }
            else if (key.Key == Key.F3)
            {
                _showDebug = !_showDebug;
            }
            else if (key.Key == Key.F5)
            {
                _showBiomeMenu = true;
            }
            else if (key.Key == Key.F11)
            {
                ToggleFullscreen();
            }
        };

        BiomeRegistry.LoadColorMap(_assets);
        _seed = WorldSave.LoadSeed(config.WorldPath, config.Seed);
        WorldSave.SaveSeed(config.WorldPath, _seed);
        _session = new WorldSession(_seed, _config, _assets, _renderDevice);
        var settings = _session.Settings;
        _changeQueue = _session.ChangeQueue;
        _worldEditor = _session.Editor;
        _saveQueue = _session.SaveQueue;

        _input = new InputState();
        _inputHandler = new InputHandler(_input);
        _inputHandler.Attach(_renderDevice.Window);
        _inputHandler.SetRelative(true);
        _showDebug = _config.ShowDebug;
        _paused = false;

        _inventory = new Inventory();
        var playerPath = System.IO.Path.Combine(config.WorldPath, "player.dat");
        var bootstrapGenerator = new WorldGenerator(_seed, settings);
        Vector3 startPos;
        bool loadedPlayer = false;
        var playerFallback = new PlayerSaveData(new Vector3(0, 80, 0), _inventory.Hotbar, _inventory.SelectedIndex);
        PlayerSaveData playerData = playerFallback;
        if (System.IO.File.Exists(playerPath))
        {
            loadedPlayer = true;
            playerData = WorldSave.LoadPlayer(config.WorldPath, playerFallback);
            startPos = playerData.Position;
            int surface = bootstrapGenerator.EstimateSurfaceHeight((int)startPos.X, (int)startPos.Z);
            float safeY = Math.Max(surface + 3f, settings.SeaLevel + 4f);
            if (startPos.Y < safeY)
            {
                startPos = new Vector3(startPos.X, safeY, startPos.Z);
            }
        }
        else
        {
            if (SpawnLocator.TryFindSpawn(_seed, settings, _config.Spawn, out var spawn, out var biome))
            {
                startPos = spawn;
                Log.Info($"Spawned in biome {biome} at {startPos.X:F1},{startPos.Y:F1},{startPos.Z:F1}");
            }
            else
            {
                int spawnX = Chunk.SizeX / 2;
                int spawnZ = Chunk.SizeZ / 2;
                int surface = bootstrapGenerator.EstimateSurfaceHeight(spawnX, spawnZ);
                float safeY = Math.Max(surface + 3f, settings.SeaLevel + 4f);
                startPos = new Vector3(spawnX + 0.5f, safeY, spawnZ + 0.5f);
                Log.Warn("SpawnLocator failed; fallback to origin spawn.");
            }
        }

        var spawnCoord = WorldType.ToChunkCoord((int)startPos.X, (int)startPos.Z);
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                var coord = new ChunkCoord(spawnCoord.X + dx, spawnCoord.Z + dz);
                if (_session.World.GetChunk(coord) != null)
                {
                    continue;
                }

                var spawnChunk = new Chunk(coord.X, coord.Z);
                lock (spawnChunk.SyncRoot)
                {
                    bootstrapGenerator.GenerateChunk(spawnChunk);
                }
                _session.World.SetChunk(spawnChunk);
                _session.Renderer.EnqueueChunk(spawnChunk, ChunkNeighborhood.FromWorld(_session.World, spawnChunk));
            }
        }
        _player = new Player(startPos, _config.Physics);
        _camera = new Camera
        {
            Position = _player.Position + new Vector3(0, _config.Physics.EyeHeight, 0),
            Yaw = _player.Yaw,
            Pitch = _player.Pitch,
            Fov = _config.FieldOfView
        };

        if (loadedPlayer)
        {
            _inventory.ApplySave(playerData.Hotbar, playerData.SelectedIndex);
        }
        _hud = new HudState { Health = 20, MaxHealth = 20 };

        string langPath = config.Language == "ru" ? "lang_ru.json" : "lang_en.json";
        _localization = new Localization(_assets, langPath);

        _dayNight = new DayNightCycle(_config.DayNight);
        _weather = new WeatherSystem(_config.Weather);

        _soundRegistry = new SoundRegistry(_assets);
        var audioBackend = AudioBackendFactory.Create(_config.Audio);
        _soundSystem = new SoundSystem(_assets, audioBackend, _config.Audio);

        RunAssetAudit();
        BuildBiomeMenuItems();

        if (_config.ShowDebug)
        {
            SpawnLocator.LogBiomeSample(_seed, settings, startPos, 32);
        }
    }

    private void RunAssetAudit()
    {
        var audit = new AssetAudit(_assets);
        var result = audit.Run();

        Log.Info($"Assets: textures={result.TextureCount}, sounds={result.SoundCount}, fonts={result.FontCount}, localization={result.LocalizationCount}");
        Log.Info($"Sounds loaded: {_soundRegistry.Sounds.Count}");
        if (result.MissingTextures.Count > 0)
        {
            var sample = string.Join(", ", result.MissingTextures.Distinct().Take(12));
            Log.Warn($"Missing textures: {sample}");
        }
    }

    public void Run()
    {
        var stopwatch = Stopwatch.StartNew();
        while (_renderDevice.Window.Exists && !_exitRequested)
        {
            _renderDevice.Window.PumpEvents();
            if (!_renderDevice.Window.Exists)
            {
                break;
            }
            _inputHandler.UpdateMouseDelta(_renderDevice.Window);

            if (_pendingRegen)
            {
                RegenerateWorld(_pendingRegenBiome);
                _pendingRegen = false;
                _pendingRegenBiome = null;
                _input.ResetDeltas();
                continue;
            }

            float dt = (float)stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            if (dt > 0.05f)
            {
                dt = 0.05f;
            }

            _fpsTimer += dt;
            _fpsFrames++;
            if (_fpsTimer >= 1f)
            {
                _fps = _fpsFrames / _fpsTimer;
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }

            _renderDevice.ResizeIfNeeded();

            bool simulate = !_paused && !_showBiomeMenu;
            if (simulate)
            {
                _dayNight.Update(dt);
                _weather.Update(dt);
                _timeSeconds += dt;

                _player.Update(dt, _input, _session.World, _config.PlayerSpeed, _config.MouseSensitivity);
                _camera.Position = _player.Position + new Vector3(0, _config.Physics.EyeHeight, 0);
                _camera.Yaw = _player.Yaw;
                _camera.Pitch = _player.Pitch;
                _camera.AspectRatio = _renderDevice.Window.Width / (float)_renderDevice.Window.Height;

                _session.ChunkManager.Update(_player.Position, _config.ChunkRadius, _config.ChunkPreloadExtra);
                _session.Renderer.UpdateMeshes();

                HandleBlockInteraction();
                _session.ChunkManager.ProcessChanges(_changeQueue, _saveQueue);
                HandleAutosave(dt);

                if (_input.MouseWheelDelta != 0)
                {
                    _inventory.Scroll(_input.MouseWheelDelta);
                }
            }
            else
            {
                _session.Renderer.UpdateMeshes();
                _session.ChunkManager.ProcessChanges(_changeQueue, _saveQueue);
            }

            _hud.SelectedSlot = _inventory.SelectedIndex;
            _hud.HotbarSize = _inventory.Hotbar.Length;
            _hud.DebugText = _showDebug ? BuildDebugText() : string.Empty;
            _hud.MenuText = _showBiomeMenu ? BuildBiomeMenuText() : string.Empty;

            var atmosphere = BuildAtmosphereFrame();
            _session.Renderer.Draw(_camera, _hud, atmosphere);

            _input.ResetDeltas();
        }

        WorldSave.SavePlayer(_config.WorldPath, new PlayerSaveData(_player.Position, _inventory.Hotbar, _inventory.SelectedIndex));
        _session.ChunkManager.SaveAll();
        _saveQueue.Flush();
    }

    private AtmosphereFrame BuildAtmosphereFrame()
    {
        float sun = _dayNight.GetSunIntensity();
        var dayTop = ColorSpace.ToLinear(_config.Atmosphere.SkyDayTop.ToVector3());
        var dayBottom = ColorSpace.ToLinear(_config.Atmosphere.SkyDayBottom.ToVector3());
        var nightTop = ColorSpace.ToLinear(_config.Atmosphere.SkyNightTop.ToVector3());
        var nightBottom = ColorSpace.ToLinear(_config.Atmosphere.SkyNightBottom.ToVector3());

        var skyTop = Vector3.Lerp(nightTop, dayTop, sun);
        var skyBottom = Vector3.Lerp(nightBottom, dayBottom, sun);

        var sky = new Vector4(skyBottom, 1f);
        var fog = _weather.Apply(sky);

        float cloudOffset = _timeSeconds * _config.Atmosphere.CloudSpeed;
        return new AtmosphereFrame(skyTop, skyBottom, fog, sun, _timeSeconds, _dayNight.TimeOfDay, cloudOffset);
    }

    private void HandleAutosave(float dt)
    {
        if (!_config.SaveSettings.EnableAutoSave)
        {
            return;
        }

        _autoSaveTimer += dt;
        if (_autoSaveTimer < _config.SaveSettings.AutoSaveIntervalSeconds)
        {
            return;
        }

        _autoSaveTimer = 0f;
        _saveQueue.EnqueueDirty(_session.World.Chunks);
        WorldSave.SavePlayer(_config.WorldPath, new PlayerSaveData(_player.Position, _inventory.Hotbar, _inventory.SelectedIndex));
    }

    private string BuildDebugText()
    {
        var pos = _player.Position;
        var chunk = WorldType.ToChunkCoord((int)pos.X, (int)pos.Z);
        int chunks = _session.World.Chunks.Count();
        return $"FPS: {_fps:F0}\n{_localization.Get("hud.seed")}: {_session.World.Seed}\n{_localization.Get("hud.position")}: {pos.X:F1} {pos.Y:F1} {pos.Z:F1}\nChunk: {chunk.X}, {chunk.Z}\nChunks: {chunks}\nPaused: {_paused}";
    }

    private void BuildBiomeMenuItems()
    {
        _biomeMenuItems.Clear();
        _biomeMenuItems.Add(new BiomeMenuItem("Any", null));
        foreach (BiomeId id in Enum.GetValues(typeof(BiomeId)))
        {
            _biomeMenuItems.Add(new BiomeMenuItem(id.ToString(), id));
        }

        _biomeMenuIndex = 0;
        if (_session.Settings.ForcedBiome.HasValue)
        {
            for (int i = 0; i < _biomeMenuItems.Count; i++)
            {
                if (_biomeMenuItems[i].Biome == _session.Settings.ForcedBiome.Value)
                {
                    _biomeMenuIndex = i;
                    break;
                }
            }
        }
    }

    private void StepBiomeMenu(int delta)
    {
        if (_biomeMenuItems.Count == 0)
        {
            return;
        }

        int count = _biomeMenuItems.Count;
        _biomeMenuIndex = (_biomeMenuIndex + delta) % count;
        if (_biomeMenuIndex < 0)
        {
            _biomeMenuIndex += count;
        }
    }

    private void QueueWorldRegeneration()
    {
        if (_biomeMenuItems.Count == 0)
        {
            return;
        }

        _pendingRegenBiome = _biomeMenuItems[_biomeMenuIndex].Biome;
        _pendingRegen = true;
        _showBiomeMenu = false;
    }

    private string BuildBiomeMenuText()
    {
        if (_biomeMenuItems.Count == 0)
        {
            return "No biomes available.";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Select biome:");
        for (int i = 0; i < _biomeMenuItems.Count; i++)
        {
            sb.Append(i == _biomeMenuIndex ? "> " : "  ");
            sb.AppendLine(_biomeMenuItems[i].Label);
        }
        sb.AppendLine("Enter: regenerate  Esc/F5: close");
        return sb.ToString();
    }

    private void RegenerateWorld(BiomeId? forcedBiome)
    {
        _showBiomeMenu = false;
        _paused = false;
        _pendingRegen = false;

        _config.WorldGen.ForcedBiome = forcedBiome?.ToString();
        GameConfig.Save("config.json", _config);

        ResetWorldFiles();

        _session.Dispose();
        _session = new WorldSession(_seed, _config, _assets, _renderDevice);
        _changeQueue = _session.ChangeQueue;
        _worldEditor = _session.Editor;
        _saveQueue = _session.SaveQueue;

        var settings = _session.Settings;
        var bootstrapGenerator = new WorldGenerator(_seed, settings);
        Vector3 startPos;
        if (SpawnLocator.TryFindSpawn(_seed, settings, _config.Spawn, out var spawn, out var biome))
        {
            startPos = spawn;
            Log.Info($"Spawned in biome {biome} at {startPos.X:F1},{startPos.Y:F1},{startPos.Z:F1}");
        }
        else
        {
            int spawnX = Chunk.SizeX / 2;
            int spawnZ = Chunk.SizeZ / 2;
            int surface = bootstrapGenerator.EstimateSurfaceHeight(spawnX, spawnZ);
            float safeY = Math.Max(surface + 3f, settings.SeaLevel + 4f);
            startPos = new Vector3(spawnX + 0.5f, safeY, spawnZ + 0.5f);
            Log.Warn("SpawnLocator failed; fallback to origin spawn.");
        }

        var spawnCoord = WorldType.ToChunkCoord((int)startPos.X, (int)startPos.Z);
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                var coord = new ChunkCoord(spawnCoord.X + dx, spawnCoord.Z + dz);
                if (_session.World.GetChunk(coord) != null)
                {
                    continue;
                }

                var spawnChunk = new Chunk(coord.X, coord.Z);
                lock (spawnChunk.SyncRoot)
                {
                    bootstrapGenerator.GenerateChunk(spawnChunk);
                }
                _session.World.SetChunk(spawnChunk);
                _session.Renderer.EnqueueChunk(spawnChunk, ChunkNeighborhood.FromWorld(_session.World, spawnChunk), highPriority: true);
            }
        }

        _player = new Player(startPos, _config.Physics);
        _camera.Position = _player.Position + new Vector3(0, _config.Physics.EyeHeight, 0);
        _camera.Yaw = _player.Yaw;
        _camera.Pitch = _player.Pitch;
        _camera.Fov = _config.FieldOfView;

        _inventory.Reset();
        _dayNight.Reset();
        _weather.Reset();
        _timeSeconds = 0f;
        _autoSaveTimer = 0f;

        BuildBiomeMenuItems();
    }

    private void ResetWorldFiles()
    {
        try
        {
            if (System.IO.Directory.Exists(_config.WorldPath))
            {
                System.IO.Directory.Delete(_config.WorldPath, true);
            }
            System.IO.Directory.CreateDirectory(_config.WorldPath);
            WorldSave.SaveSeed(_config.WorldPath, _seed);
            Log.Warn("World reset: cleared existing world data.");
        }
        catch (Exception ex)
        {
            Log.Warn($"World reset failed: {ex.Message}");
        }
    }

    private void TogglePause()
    {
        _paused = !_paused;
        _renderDevice.Window.CursorVisible = _paused;
        SetMouseMode(captured: true, relative: !_paused);
        _inputHandler.SetRelative(!_paused);
        _input.ResetDeltas();
        _inputHandler.ResetMouse();
    }

    private void ToggleFullscreen()
    {
        var window = _renderDevice.Window;
        if (window.WindowState == WindowState.BorderlessFullScreen)
        {
            window.WindowState = WindowState.Normal;
        }
        else
        {
            window.WindowState = WindowState.BorderlessFullScreen;
        }
    }

    private void SetMouseMode(bool captured, bool relative)
    {
        var handle = _renderDevice.Window.SdlWindowHandle;
        Sdl2Native.SDL_SetWindowGrab(handle, captured);
        Sdl2Native.SDL_SetRelativeMouseMode(relative);
    }

    private void HandleBlockInteraction()
    {
        var forward = _camera.GetForward();
        var result = WorldRaycaster.Raycast(_session.World, _camera.Position, forward, 6f);

        if (_input.MouseLeft && !_leftHandled)
        {
            if (result.Hit)
            {
                var block = _session.World.GetBlock(result.Block.X, result.Block.Y, result.Block.Z);
                _worldEditor.SetBlock(result.Block.X, result.Block.Y, result.Block.Z, BlockId.Air);
                _soundSystem.PlayDig(block);
            }
            _leftHandled = true;
        }
        if (!_input.MouseLeft)
        {
            _leftHandled = false;
        }

        if (_input.MouseRight && !_rightHandled)
        {
            if (result.Hit)
            {
                var block = _inventory.GetSelectedBlock();
                if (_session.World.GetBlock(result.Adjacent.X, result.Adjacent.Y, result.Adjacent.Z) == BlockId.Air &&
                    !_player.IntersectsBlock(result.Adjacent))
                {
                    _worldEditor.SetBlock(result.Adjacent.X, result.Adjacent.Y, result.Adjacent.Z, block);
                    _soundSystem.PlayPlace(block);
                }
            }
            _rightHandled = true;
        }
        if (!_input.MouseRight)
        {
            _rightHandled = false;
        }
    }

    public void Dispose()
    {
        _session.Dispose();
        _soundSystem.Dispose();
        _renderDevice.Dispose();
    }
}
