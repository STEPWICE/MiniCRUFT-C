using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    private const string VersionLabel = "MiniCRUFT Alpha v1.0.16_02";

    private readonly GameConfig _config;
    private readonly string _configPath;
    private readonly AssetStore _assets;
    private readonly RenderDevice _renderDevice;
    private WorldSession _session;
    private WorldEditor _worldEditor;
    private IWorldChangeQueue _changeQueue;
    private ChunkSaveQueue _saveQueue;
    private Player _player;
    private readonly Camera _camera = new();
    private readonly InputState _input;
    private readonly InputHandler _inputHandler;
    private readonly Inventory _inventory;
    private readonly string[] _inventoryLabels;
    private readonly BlockId[] _inventoryItems;
    private readonly HudState _hud;
    private readonly Localization _localization;
    private readonly DayNightCycle _dayNight;
    private readonly WeatherSystem _weather;
    private readonly SoundSystem _soundSystem;
    private readonly AmbientSoundSystem _ambientSoundSystem;
    private readonly MobSoundSystem _mobSoundSystem;
    private readonly PlayerFeedbackSystem _playerFeedback;
    private readonly CraftingSystem _craftingSystem = new();
    private readonly SmeltingSystem _smeltingSystem = new();
    private readonly HarvestSystem _harvestSystem;
    private readonly ToolRepairSystem _toolRepairSystem;
    private readonly ProgressionGuideSystem _progressionGuideSystem = new();
    private readonly ProgressionMilestoneSystem _progressionMilestoneSystem = new();
    private readonly BlockBreakSystem _blockBreakSystem = new();
    private readonly HungerSystem _hungerSystem;
    private readonly SleepSystem _sleepSystem;
    private HerobrineSystem _herobrine;
    private readonly CameraMotionState _cameraMotion = new();
    private readonly List<TntRenderInstance> _tntRenderInstances = new();
    private readonly List<LootDrop> _lootDrops = new();
    private readonly IAudioBackend _audioBackend;
    private readonly List<BiomeMenuItem> _biomeMenuItems = new();
    private bool _showBiomeMenu;
    private int _biomeMenuIndex;
    private bool _showSeedMenu;
    private bool _showSettingsMenu;
    private bool _showAudioSettingsMenu;
    private string _seedInput = string.Empty;
    private bool _inventoryOpen;
    private bool _pendingRegen;
    private BiomeId? _pendingRegenBiome;
    private int? _pendingSeed;
    private bool _exitRequested;
    private Vector3 _spawnPosition;
    private float _timeSeconds;
    private bool _paused;
    private int _pauseMenuIndex;
    private int _settingsMenuIndex;
    private int _audioSettingsMenuIndex;
    private bool _showDebug;
    private float _fpsTimer;
    private int _fpsFrames;
    private float _fps;
    private float _autoSaveTimer;
    private float _herobrineToastTimer;
    private string _herobrineToastText = string.Empty;
    private float _statusToastTimer;
    private string _statusToastText = string.Empty;
    private const float MouseSensitivityStep = 0.01f;
    private const float FieldOfViewStep = 5f;
    private const float AudioVolumeStep = 0.05f;

    private bool _leftHandled;
    private bool _rightHandled;
    private int _seed;
    private float _itemNameTimer;
    private int _lastSelectedSlot;
    private float _playerAttackTimer;
    private float _firstPersonSwingTimer;
    private SelectionState _selectionState;
    private bool _shutdownSaved;
    private int _windowedX;
    private int _windowedY;
    private int _windowedWidth;
    private int _windowedHeight;

    public GameApp(GameConfig config, string configPath, bool centerWindowOnStart)
    {
        _config = config;
        _configPath = configPath;
        _harvestSystem = new HarvestSystem(_config.Tools);
        _toolRepairSystem = new ToolRepairSystem(_config.Tools);
        _assets = new AssetStore(config.AssetsPath);
        RunAssetAudit();
        BiomeRegistry.LoadColorMap(_assets, _config.StrictBetaMode);
        _windowedX = config.WindowX;
        _windowedY = config.WindowY;
        _windowedWidth = config.WindowWidth;
        _windowedHeight = config.WindowHeight;
        _renderDevice = RenderDevice.Create(VersionLabel, config.WindowX, config.WindowY, config.WindowWidth, config.WindowHeight, config.VSync, config.Fullscreen, centerWindowOnStart);

        _renderDevice.Window.CursorVisible = false;
        _renderDevice.Window.Moved += _ => CaptureWindowPlacementIfNeeded();
        _renderDevice.Window.Resized += CaptureWindowPlacementIfNeeded;
        CaptureWindowPlacementIfNeeded();
        SetMouseMode(captured: true, relative: true);
        _renderDevice.Window.KeyDown += key =>
        {
            if (_showBiomeMenu)
            {
                if (_config.StrictBetaMode)
                {
                    _showBiomeMenu = false;
                    SyncCursorState();
                    return;
                }

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
                    case Key.F6:
                        OpenSeedMenu();
                        break;
                    case Key.Escape:
                    case Key.F5:
                        _showBiomeMenu = false;
                        SyncCursorState();
                        break;
                }
                return;
            }

            if (_showSeedMenu)
            {
                if (_config.StrictBetaMode)
                {
                    _showSeedMenu = false;
                    SyncCursorState();
                    return;
                }

                HandleSeedMenuKey(key);
                return;
            }

            if (_showAudioSettingsMenu)
            {
                if (_config.StrictBetaMode)
                {
                    _showAudioSettingsMenu = false;
                    _showSettingsMenu = false;
                    SyncCursorState();
                    return;
                }

                HandleAudioSettingsMenuKey(key);
                return;
            }

            if (_showSettingsMenu)
            {
                if (_config.StrictBetaMode)
                {
                    _showSettingsMenu = false;
                    SyncCursorState();
                    return;
                }

                HandleSettingsMenuKey(key);
                return;
            }

            if (_paused)
            {
                HandlePauseMenuKey(key);
                return;
            }

            if (_inventoryOpen)
            {
                if (key.Key == Key.Escape || key.Key == Key.E)
                {
                    _inventoryOpen = false;
                    SyncCursorState();
                    return;
                }

                if (key.Key == Key.C)
                {
                    if (TryCraftInventory())
                    {
                        UpdateInventoryLabels();
                        UpdateInventoryItems();
                    }
                    return;
                }

                if (key.Key == Key.V)
                {
                    if (TrySmeltInventory())
                    {
                        UpdateInventoryLabels();
                        UpdateInventoryItems();
                    }
                    return;
                }

                if (key.Key == Key.R)
                {
                    if (TryRepairInventory())
                    {
                        UpdateInventoryLabels();
                        UpdateInventoryItems();
                    }
                }
                return;
            }

            if (key.Key == Key.Escape)
            {
                if (_paused)
                {
                    TogglePause();
                }
                else
                {
                    _exitRequested = true;
                }
            }
            else if (!_config.StrictBetaMode && key.Key == Key.Tab)
            {
                TogglePause();
            }
            else if (key.Key == Key.E)
            {
                _inventoryOpen = true;
                SyncCursorState();
            }
            else if (!_config.StrictBetaMode && key.Key == Key.R)
            {
                TryRest();
            }
            else if (!_config.StrictBetaMode && key.Key == Key.F3)
            {
                _showDebug = !_showDebug;
            }
            else if (!_config.StrictBetaMode && key.Key == Key.F5)
            {
                _showBiomeMenu = true;
                SyncCursorState();
            }
            else if (!_config.StrictBetaMode && key.Key == Key.F6)
            {
                OpenSeedMenu();
            }
            else if (!_config.StrictBetaMode && key.Key == Key.F8)
            {
                ToggleHerobrine();
            }
            else if (!_config.StrictBetaMode && key.Key == Key.F9)
            {
                CycleHerobrineMode();
            }
            else if (key.Key == Key.F11)
            {
                ToggleFullscreen();
            }
        };

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
        _showDebug = !_config.StrictBetaMode && _config.ShowDebug;
        _paused = false;

        _inventory = new Inventory(_config.StrictBetaMode, _config.Tools);
        int invCols = Math.Max(1, _config.Ui.InventoryColumns);
        int invRows = Math.Max(1, _config.Ui.InventoryRows);
        _inventoryLabels = new string[invCols * invRows + _inventory.Hotbar.Length];
        _inventoryItems = new BlockId[invCols * invRows + _inventory.Hotbar.Length];
        UpdateInventoryLabels();
        UpdateInventoryItems();
        var playerPath = System.IO.Path.Combine(config.WorldPath, "player.dat");
        var bootstrapGenerator = new WorldGenerator(_seed, settings);
        Vector3 startPos;
        bool loadedPlayer = false;
        var playerFallback = new PlayerSaveData(new Vector3(0, 80, 0), _inventory.Hotbar, _inventory.HotbarCounts, _inventory.HotbarDurability, _inventory.SelectedIndex);
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
                Log.Info($"Spawned in biome {BiomeRegistry.Get(biome).Name} at {startPos.X:F1},{startPos.Y:F1},{startPos.Z:F1}");
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
        _spawnPosition = startPos;
        _player = new Player(startPos, _config.Physics);
        _cameraMotion.Reset();
        SyncCameraToPlayer();

        if (loadedPlayer)
        {
            if (!_config.StrictBetaMode)
            {
                _inventory.ApplySave(playerData.Hotbar, playerData.Counts, playerData.ToolDurability, playerData.SelectedIndex);
                EnsureTntAccessible();
            }
        }
        UpdateInventoryLabels();
        UpdateInventoryItems();
        LoadSavedMobs();
        LoadSavedTnt();
        _herobrine = new HerobrineSystem(_seed, _config.Herobrine);
        LoadSavedHerobrine();
        _hungerSystem = new HungerSystem(_config.Survival);
        _sleepSystem = new SleepSystem(_config.Survival);
        _dayNight = new DayNightCycle(_config.DayNight);
        _weather = new WeatherSystem(_config.Weather);
        LoadSavedDayNight();
        LoadSavedWeather();
        LoadSavedHunger();

        _hud = new HudState
        {
            Health = _player.Health,
            MaxHealth = _player.MaxHealth,
            SurvivalEnabled = _config.Survival.Enabled,
            Hunger = (int)MathF.Ceiling(_hungerSystem.Hunger),
            MaxHunger = _hungerSystem.MaxHunger,
            HotbarDurability = _inventory.HotbarDurability,
            HotbarMaxDurability = _inventory.HotbarMaxDurability,
            VersionText = VersionLabel
        };
        _lastSelectedSlot = _inventory.SelectedIndex;
        _itemNameTimer = _config.Ui.ItemNameDisplaySeconds;

        string langPath = config.Language == "ru" ? "lang_ru.json" : "lang_en.json";
        _localization = new Localization(_assets, langPath);

        _audioBackend = AudioBackendFactory.Create(_config.Audio);
        _soundSystem = new SoundSystem(_assets, _audioBackend, _config.Audio, ownsBackend: false);
        _ambientSoundSystem = new AmbientSoundSystem(_assets, _audioBackend, _config.Audio);
        _mobSoundSystem = new MobSoundSystem(_assets, _audioBackend, _config.Audio);
        _playerFeedback = new PlayerFeedbackSystem(_soundSystem, _session.Renderer, _config.Audio);
        _playerFeedback.Reset(_player);
        SyncAudioListener();

        BuildBiomeMenuItems();

        if (_config.ShowDebug)
        {
            SpawnLocator.LogBiomeSample(_seed, settings, startPos, 32);
        }
    }

    private void RunAssetAudit()
    {
        var audit = new AssetAudit(_assets);
        var result = audit.Run(_config.StrictBetaMode);

        Log.Info($"Assets: textures={result.TextureCount} (block={result.BlockTextureCount}, water={result.WaterTextureCount}, item={result.ItemTextureCount}, entity={result.EntityTextureCount}, hud={result.HudTextureCount}, colormap={result.ColormapCount}), sounds={result.SoundCount} (mob={result.MobSoundCount}), fonts={result.FontCount}, localization={result.LocalizationCount}");
        if (result.MissingTextures.Count > 0)
        {
            var sample = string.Join(", ", result.MissingTextures.Distinct().Take(12));
            Log.Warn($"Missing textures: {sample}");
        }

        if (result.PlaceholderTextures.Count > 0)
        {
            var sample = string.Join(", ", result.PlaceholderTextures.Distinct().Take(12));
            Log.Warn($"Placeholder textures: {sample}");
        }

        if (result.MobModelIssues.Count > 0)
        {
            var sample = string.Join(", ", result.MobModelIssues.Distinct().Take(12));
            Log.Warn($"Mob model issues: {sample}");
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
                RegenerateWorld(_pendingRegenBiome, _pendingSeed);
                _pendingRegen = false;
                _pendingRegenBiome = null;
                _pendingSeed = null;
                _input.ResetDeltas();
                continue;
            }

            float dt = (float)stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            if (dt > 0.05f)
            {
                dt = 0.05f;
            }

            float sunIntensity = _dayNight.GetSunIntensity();
            _fpsTimer += dt;
            _fpsFrames++;
            if (_fpsTimer >= 1f)
            {
                _fps = _fpsFrames / _fpsTimer;
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }

            _renderDevice.ResizeIfNeeded();

            bool simulate = !_paused && !_showBiomeMenu && !_showSeedMenu && !_inventoryOpen;
            simulate = simulate && !_showSettingsMenu && !_showAudioSettingsMenu;
            if (simulate)
            {
                _dayNight.Update(dt);
                sunIntensity = _dayNight.GetSunIntensity();
                _weather.Update(dt, sunIntensity);
                _timeSeconds += dt;
                bool movementIntent = _input.Forward || _input.Backward || _input.Left || _input.Right;
                bool canSprint = _hungerSystem.CanSprint;
                float sprintMultiplier = canSprint ? _config.Physics.SprintMultiplier : 1f;
                bool sprinting = movementIntent && _input.Sprint && canSprint;

                // Prime nearby chunks before physics resolves movement against world edges.
                _session.ChunkManager.Update(_player.Position, _config.ChunkRadius, _config.ChunkPreloadExtra);

                _player.Update(dt, _input, _session.World, _config.PlayerSpeed, _config.MouseSensitivity, sprintMultiplier);
                _playerAttackTimer = Math.Max(0f, _playerAttackTimer - dt);
                _firstPersonSwingTimer = Math.Max(0f, _firstPersonSwingTimer - dt);
                UpdateCameraMotion(dt, sprintMultiplier);
                SyncCameraToPlayer();
                SyncAudioListener();
                _playerFeedback.Update(dt, _player, _input, _session.World, canSprint);

                // Refresh after movement so newly entered chunks become render-ready this frame.
                _session.ChunkManager.Update(_player.Position, _config.ChunkRadius, _config.ChunkPreloadExtra);
                _ambientSoundSystem.Update(dt, _session.World, _player.Position, _weather.RainIntensity);
                while (_weather.TryConsumeThunderEvent())
                {
                    _ambientSoundSystem.PlayThunder();
                }

                HandleBlockInteraction(dt);
                _hungerSystem.Update(dt, _player, sprinting, _player.InWater || _player.InLava);
                _session.Mobs.Update(
                    dt,
                    _session.World,
                    _session.Editor,
                    _player,
                    sunIntensity,
                    _weather.RainIntensity,
                    primeTnt: block => _session.TntSystem.Prime(_session.World, block, _config.Tnt.ChainReactionFuseSeconds));
                _herobrine.Update(dt, _session.World, _session.Editor, _session.Mobs, _player, _camera, sunIntensity);
                _session.TntSystem.Update(dt, _session.World, _session.Editor, _player, _session.Mobs);
                _session.FireSystem.Update(_session.World, _session.Editor, dt, _weather.RainIntensity);
                _session.Fluids.Update(_session.World, _session.Editor);
                _session.FallingBlocks.Update(_session.World, _session.Editor);
                HandlePlayerDeath();
                SyncCameraToPlayer();
                SyncAudioListener();
                HandleHerobrineCues();
                while (_session.Mobs.TryDequeueEvent(out var mobEvent))
                {
                    HandleMobEvent(mobEvent);
                }
                while (_session.TntSystem.TryDequeueEvent(out var tntEvent))
                {
                    HandleTntEvent(tntEvent);
                }
                _session.ChunkManager.ProcessChanges(_changeQueue, _saveQueue, _session.FallingBlocks, _session.Fluids, _session.FireSystem, _session.TntSystem);
                while (_session.FireSystem.TryDequeueEvent(out var fireEvent))
                {
                    HandleFireEvent(fireEvent);
                }
                _session.Renderer.UpdateMeshes();
                _session.Renderer.Update(dt, _weather.RainIntensity, _renderDevice.Window.Width, _renderDevice.Window.Height);
                HandleAutosave(dt);

                if (_input.MouseWheelDelta != 0)
                {
                    _inventory.Scroll(_input.MouseWheelDelta);
                }
            }
            else
            {
                _blockBreakSystem.Cancel();
                _selectionState = SelectionState.None;
                _session.ChunkManager.ProcessChanges(_changeQueue, _saveQueue, _session.FallingBlocks, _session.Fluids, _session.FireSystem, _session.TntSystem);
                _session.Renderer.UpdateMeshes();
                SyncCameraToPlayer();
            }

            if (_inventory.SelectedIndex != _lastSelectedSlot)
            {
                _lastSelectedSlot = _inventory.SelectedIndex;
                _itemNameTimer = _config.Ui.ItemNameDisplaySeconds;
            }
            else if (_itemNameTimer > 0f)
            {
                _itemNameTimer = Math.Max(0f, _itemNameTimer - dt);
            }

            if (_herobrineToastTimer > 0f)
            {
                _herobrineToastTimer = Math.Max(0f, _herobrineToastTimer - dt);
            }

            if (_statusToastTimer > 0f)
            {
                _statusToastTimer = Math.Max(0f, _statusToastTimer - dt);
            }

            _hud.SelectedSlot = _inventory.SelectedIndex;
            _hud.HotbarSize = _inventory.Hotbar.Length;
            _hud.WorldTimeSeconds = _timeSeconds;
            _hud.PlayerSpeed = _player.Velocity.Length();
            _hud.Health = _player.Health;
            _hud.MaxHealth = _player.MaxHealth;
            _hud.HotbarItems = _inventory.Hotbar;
            _hud.HotbarCounts = _inventory.HotbarCounts;
            _hud.HotbarDurability = _inventory.HotbarDurability;
            _hud.HotbarMaxDurability = _inventory.HotbarMaxDurability;
            _hud.InventoryItems = _inventoryItems;
            _hud.ProgressionMilestonesText = BuildProgressionMilestonesText();
            _hud.DebugText = _showDebug ? BuildDebugText() : string.Empty;
            _hud.MenuText = _showBiomeMenu ? BuildBiomeMenuText() : _showSeedMenu ? BuildSeedMenuText() : _showAudioSettingsMenu ? BuildAudioSettingsMenuText() : _showSettingsMenu ? BuildSettingsMenuText() : _inventoryOpen ? BuildInventoryMenuText() : _paused ? BuildPauseMenuText() : string.Empty;
            _hud.HerobrineStatusText = (_paused || _showSettingsMenu || _showAudioSettingsMenu || _showBiomeMenu || _showSeedMenu || _inventoryOpen) ? string.Empty : BuildHerobrineStatusText();
            _hud.HerobrineToastText = _herobrineToastTimer > 0f ? _herobrineToastText : string.Empty;
            _hud.ProgressionText = BuildProgressionText();
            _hud.SelectedItemName = (_inventoryOpen || _itemNameTimer <= 0f) ? string.Empty : BuildSelectedItemName();
            _hud.StrictBetaMode = _config.StrictBetaMode;
            _hud.InventoryOpen = _inventoryOpen;
            _hud.InventoryColumns = _config.Ui.InventoryColumns;
            _hud.InventoryRows = _config.Ui.InventoryRows;
            _hud.InventoryLabels = _inventoryLabels;
            _hud.StatusToastText = _statusToastTimer > 0f ? _statusToastText : string.Empty;

            var atmosphere = BuildAtmosphereFrame(sunIntensity);
            _session.TntSystem.FillRenderInstances(_tntRenderInstances);
            var firstPersonState = BuildFirstPersonRenderState();
            _session.Renderer.Draw(_camera, _hud, atmosphere, _session.Mobs.RenderInstances, _tntRenderInstances, _selectionState, firstPersonState);

            _input.ResetDeltas();
        }

        PersistShutdownState();
    }

    private AtmosphereFrame BuildAtmosphereFrame(float sunIntensity)
    {
        float rain = _weather.RainIntensity;
        float moon = Math.Clamp(1f - sunIntensity, 0.1f, 1f);
        int moonPhase = _dayNight.GetMoonPhaseIndex();
        var dayTop = ColorSpace.ToLinear(_config.Atmosphere.SkyDayTop.ToVector3());
        var dayBottom = ColorSpace.ToLinear(_config.Atmosphere.SkyDayBottom.ToVector3());
        var nightTop = ColorSpace.ToLinear(_config.Atmosphere.SkyNightTop.ToVector3());
        var nightBottom = ColorSpace.ToLinear(_config.Atmosphere.SkyNightBottom.ToVector3());
        var fogDay = ColorSpace.ToLinear(_config.Atmosphere.FogDayColor.ToVector3());
        var fogNight = ColorSpace.ToLinear(_config.Atmosphere.FogNightColor.ToVector3());
        var fogRain = ColorSpace.ToLinear(_config.Atmosphere.FogRainColor.ToVector3());

        var skyTop = Vector3.Lerp(nightTop, dayTop, sunIntensity);
        var skyBottom = Vector3.Lerp(nightBottom, dayBottom, sunIntensity);
        skyTop = _weather.ApplySky(skyTop);
        skyBottom = _weather.ApplySky(skyBottom);

        var fog = Vector3.Lerp(fogNight, fogDay, sunIntensity);
        fog = Vector3.Lerp(fog, fogRain, rain);
        fog = Vector3.Lerp(fog, skyBottom, 0.18f);
        fog = _weather.ApplyFog(fog);

        float fogMultiplier = 1f + (_config.Atmosphere.FogNightMultiplier - 1f) * (1f - sunIntensity);
        fogMultiplier *= 1f + (_config.Atmosphere.FogRainMultiplier - 1f) * rain;
        float fogStart = MathF.Max(32f, _config.Render.FogStart * fogMultiplier);
        float fogEnd = MathF.Max(fogStart + 16f, _config.Render.FogEnd * fogMultiplier);
        float sunValue = Math.Clamp(sunIntensity * (1f - rain * 0.1f), _config.DayNight.MinSunIntensity, 1f);
        float moonIntensity = Math.Clamp(moon * (1f - rain * 0.15f), 0.1f, 1f);

        float cloudOffset = _timeSeconds * _config.Atmosphere.CloudSpeed;
        return new AtmosphereFrame(skyTop, skyBottom, new Vector4(fog, 1f), fogStart, fogEnd, sunValue, moonIntensity, moonPhase, rain, _weather.LightningFlashIntensity, _timeSeconds, _dayNight.TimeOfDay, cloudOffset);
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
        SavePersistentWorldState();
    }

    private string BuildDebugText()
    {
        var pos = _player.Position;
        var chunk = WorldType.ToChunkCoord((int)pos.X, (int)pos.Z);
        int chunks = _session.World.Chunks.Count();
        return $"FPS: {_fps:F0}\n{_localization.Get("hud.seed")}: {_session.World.Seed}\n{_localization.Get("hud.position")}: {pos.X:F1} {pos.Y:F1} {pos.Z:F1}\nChunk: {chunk.X}, {chunk.Z}\nChunks: {chunks}\nDay: {_dayNight.DayCount}  Rain: {_weather.RainIntensity:P0}\nPaused: {_paused}\n{_herobrine.BuildDebugStatus()}";
    }

    private string BuildProgressionText()
    {
        if (_config.StrictBetaMode || _paused || _showSettingsMenu || _showAudioSettingsMenu || _showBiomeMenu || _showSeedMenu || _inventoryOpen)
        {
            return string.Empty;
        }

        return _progressionGuideSystem.BuildGuideText(CreateProgressionContext());
    }

    private string BuildProgressionMilestonesText()
    {
        if (_config.StrictBetaMode || _paused || _showSettingsMenu || _showAudioSettingsMenu || _showBiomeMenu || _showSeedMenu || _inventoryOpen)
        {
            return string.Empty;
        }

        return _progressionMilestoneSystem.BuildMilestoneText(CreateProgressionContext());
    }

    private ProgressionGuideContext CreateProgressionContext()
    {
        return new ProgressionGuideContext(
            _player,
            _inventory,
            _hungerSystem,
            _sleepSystem,
            _dayNight,
            _session.Mobs,
            _craftingSystem,
            _smeltingSystem,
            _toolRepairSystem,
            _config.Survival,
            _config.StrictBetaMode);
    }

    private void UpdateInventoryLabels()
    {
        int invCols = Math.Max(1, _config.Ui.InventoryColumns);
        int invRows = Math.Max(1, _config.Ui.InventoryRows);
        int baseIndex = invCols * invRows;
        int expected = baseIndex + _inventory.Hotbar.Length;
        if (_inventoryLabels.Length != expected)
        {
            return;
        }

        Array.Clear(_inventoryLabels, 0, _inventoryLabels.Length);
        for (int i = 0; i < _inventory.Hotbar.Length; i++)
        {
            _inventoryLabels[baseIndex + i] = ShortenLabel(_inventory.Hotbar[i]);
        }
    }

    private void UpdateInventoryItems()
    {
        int invCols = Math.Max(1, _config.Ui.InventoryColumns);
        int invRows = Math.Max(1, _config.Ui.InventoryRows);
        int baseIndex = invCols * invRows;
        int expected = baseIndex + _inventory.Hotbar.Length;
        if (_inventoryItems.Length != expected)
        {
            return;
        }

        Array.Clear(_inventoryItems, 0, _inventoryItems.Length);
        for (int i = 0; i < _inventory.Hotbar.Length; i++)
        {
            _inventoryItems[baseIndex + i] = _inventory.Hotbar[i];
        }
    }

    private static string ShortenLabel(BlockId id)
    {
        if (id == BlockId.Air)
        {
            return string.Empty;
        }

        string name = id.ToString();
        if (name.Length <= 3)
        {
            return name.ToUpperInvariant();
        }

        return name.Substring(0, 3).ToUpperInvariant();
    }

    private string BuildSelectedItemName()
    {
        var id = _inventory.GetSelectedBlock();
        if (id == BlockId.Air)
        {
            return string.Empty;
        }

        string name = FormatBlockName(NormalizeBlockName(id));
        int maxDurability = _inventory.GetSelectedMaxDurability();
        if (maxDurability > 0)
        {
            int currentDurability = Math.Clamp(_inventory.GetSelectedDurability(), 0, maxDurability);
            name = $"{name} ({currentDurability}/{maxDurability})";
        }

        return name;
    }

    private static string NormalizeBlockName(BlockId id)
    {
        if (id == BlockId.Tnt)
        {
            return "TNT";
        }

        if (LiquidBlocks.IsWater(id))
        {
            return "Water";
        }

        if (LiquidBlocks.IsLava(id))
        {
            return "Lava";
        }

        return id.ToString();
    }

    private static string FormatBlockName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(raw.Length + 4);
        builder.Append(char.ToUpperInvariant(raw[0]));
        for (int i = 1; i < raw.Length; i++)
        {
            char c = raw[i];
            char prev = raw[i - 1];
            if (char.IsUpper(c) && char.IsLower(prev))
            {
                builder.Append(' ');
            }
            builder.Append(c);
        }

        return builder.ToString();
    }

    private static BlockId ResolveTorchPlacement(BlockCoord hit, BlockCoord adjacent)
    {
        int dx = adjacent.X - hit.X;
        int dy = adjacent.Y - hit.Y;
        int dz = adjacent.Z - hit.Z;

        if (dy == 1)
        {
            return BlockId.Torch;
        }

        if (dx == 1)
        {
            return BlockId.TorchWallWest;
        }

        if (dx == -1)
        {
            return BlockId.TorchWallEast;
        }

        if (dz == 1)
        {
            return BlockId.TorchWallNorth;
        }

        if (dz == -1)
        {
            return BlockId.TorchWallSouth;
        }

        return BlockId.Torch;
    }

    private void BuildBiomeMenuItems()
    {
        _biomeMenuItems.Clear();
        _biomeMenuItems.Add(new BiomeMenuItem("Any", null));
        foreach (BiomeId id in Enum.GetValues(typeof(BiomeId)))
        {
            _biomeMenuItems.Add(new BiomeMenuItem(BiomeRegistry.Get(id).Name, id));
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

    private void LoadSavedMobs()
    {
        var mobs = WorldSave.LoadMobs(_config.WorldPath);
        _session.Mobs.Load(mobs);
    }

    private void LoadSavedTnt()
    {
        var tnts = WorldSave.LoadTnt(_config.WorldPath, _config.Tnt.MaxPrimedTnt);
        _session.TntSystem.Load(tnts, _session.World);
    }

    private void LoadSavedHerobrine()
    {
        var fallback = _herobrine.BuildSaveData(_player.Position);
        var herobrine = WorldSave.LoadHerobrine(_config.WorldPath, fallback);
        _herobrine.Load(herobrine);
        if (!_config.Herobrine.Enabled)
        {
            _herobrine.Disable(_session.World, _session.Editor, _session.Mobs);
        }
    }

    private void LoadSavedDayNight()
    {
        var fallback = new DayNightSaveData(_dayNight.TimeOfDay, _dayNight.DayCount);
        var dayNight = WorldSave.LoadDayNight(_config.WorldPath, fallback);
        _dayNight.SetState(dayNight.TimeOfDay, dayNight.DayCount);
    }

    private void LoadSavedWeather()
    {
        var fallback = _weather.BuildSaveData();
        var weather = WorldSave.LoadWeather(_config.WorldPath, fallback);
        _weather.Load(weather);
    }

    private void LoadSavedHunger()
    {
        var fallback = _hungerSystem.BuildSaveData();
        var hunger = WorldSave.LoadHunger(_config.WorldPath, fallback);
        _hungerSystem.Load(hunger);
    }

    private void EnsureTntAccessible()
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        for (int i = 0; i < _inventory.Hotbar.Length; i++)
        {
            if (_inventory.Hotbar[i] == BlockId.Tnt)
            {
                return;
            }
        }

        if (_inventory.Hotbar.Length > 0)
        {
            _inventory.SetSlot(_inventory.Hotbar.Length - 1, BlockId.Tnt, BlockStackDefaults.GetDefaultCount(BlockId.Tnt));
        }
    }

    private void SavePersistentWorldState()
    {
        WorldSave.SavePlayer(_config.WorldPath, new PlayerSaveData(_player.Position, _inventory.Hotbar, _inventory.HotbarCounts, _inventory.HotbarDurability, _inventory.SelectedIndex));
        WorldSave.SaveMobs(_config.WorldPath, _session.Mobs.BuildSaveData());
        WorldSave.SaveTnt(_config.WorldPath, _session.TntSystem.BuildSaveData());
        WorldSave.SaveHerobrine(_config.WorldPath, _herobrine.BuildSaveData(_player.Position));
        WorldSave.SaveDayNight(_config.WorldPath, new DayNightSaveData(_dayNight.TimeOfDay, _dayNight.DayCount));
        WorldSave.SaveWeather(_config.WorldPath, _weather.BuildSaveData());
        WorldSave.SaveHunger(_config.WorldPath, _hungerSystem.BuildSaveData());
    }

    private void CaptureWindowPlacementIfNeeded()
    {
        var state = _renderDevice.Window.WindowState;
        if (state == WindowState.BorderlessFullScreen || state == WindowState.FullScreen ||
            state == WindowState.Minimized || state == WindowState.Hidden)
        {
            return;
        }

        _windowedX = _renderDevice.Window.X;
        _windowedY = _renderDevice.Window.Y;
        _windowedWidth = Math.Max(1, _renderDevice.Window.Width);
        _windowedHeight = Math.Max(1, _renderDevice.Window.Height);
    }

    private void SaveConfig()
    {
        CaptureWindowPlacementIfNeeded();
        _config.WindowX = _windowedX;
        _config.WindowY = _windowedY;
        _config.WindowWidth = _windowedWidth;
        _config.WindowHeight = _windowedHeight;
        var state = _renderDevice.Window.WindowState;
        _config.Fullscreen = state == WindowState.BorderlessFullScreen || state == WindowState.FullScreen;
        GameConfig.Save(_configPath, _config);
    }

    private void PersistShutdownState()
    {
        if (_shutdownSaved)
        {
            return;
        }

        _shutdownSaved = true;

        try
        {
            SavePersistentWorldState();
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save persistent world state: {ex.Message}");
        }

        try
        {
            _session.ChunkManager.SaveAll();
            _saveQueue.Flush();
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to flush chunk saves: {ex.Message}");
        }

        try
        {
            SaveConfig();
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save config on shutdown: {ex.Message}");
        }
    }

    private string BuildPauseMenuText()
    {
        return PauseHudText.BuildPauseMenuText(_pauseMenuIndex);
    }

    private string BuildSettingsMenuText()
    {
        return PauseHudText.BuildSettingsMenuText(
            _settingsMenuIndex,
            _renderDevice.Window.WindowState == WindowState.BorderlessFullScreen || _renderDevice.Window.WindowState == WindowState.FullScreen,
            _config.FieldOfView,
            _config.MouseSensitivity,
            _showDebug);
    }

    private string BuildAudioSettingsMenuText()
    {
        return PauseHudText.BuildAudioMenuText(
            _audioSettingsMenuIndex,
            _config.Audio.MasterVolume,
            _config.Audio.MusicVolume,
            _config.Audio.AmbientVolume,
            _config.Audio.WeatherVolume,
            _config.Audio.MobVolume);
    }

    private string BuildInventoryMenuText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Inventory controls:");
        sb.AppendLine("C: craft next recipe");
        sb.AppendLine("V: smelt next recipe");
        sb.AppendLine("R: repair selected tool");
        sb.AppendLine("E/Esc: close");
        if (_config.Survival.Enabled)
        {
            sb.AppendLine("Right click food: eat");
        }

        if (_craftingSystem.TryGetCraftableRecipe(_inventory, out var craftRecipe))
        {
            sb.Append("Craft: ");
            sb.AppendLine(FormatActionText(craftRecipe!.Output, craftRecipe.OutputCount));
        }
        else
        {
            sb.AppendLine("Craft: none available");
        }

        if (_smeltingSystem.TryGetSmeltableRecipe(_inventory, out var smeltRecipe))
        {
            sb.Append("Smelt: ");
            sb.AppendLine(FormatActionText(smeltRecipe!.Output, smeltRecipe.OutputCount));
        }
        else
        {
            sb.AppendLine("Smelt: none available");
        }

        if (_toolRepairSystem.TryGetRepairInfo(_inventory, out var repairInfo))
        {
            sb.Append("Repair: ");
            sb.AppendLine(
                $"{FormatActionText(repairInfo.Tool, 1)} + {FormatActionText(repairInfo.Material, repairInfo.MaterialCount)} -> +{repairInfo.RepairAmount} durability");
        }
        else
        {
            sb.AppendLine("Repair: none available");
        }

        return sb.ToString();
    }

    private string BuildHerobrineStatusText()
    {
        var modeProfile = HerobrineModeCatalog.Get(_config.Herobrine.Mode);
        return HerobrineHudText.BuildStatusText(
            _config.Herobrine.Enabled,
            modeProfile.Name,
            _herobrine.IsManifested,
            _herobrine.HauntPressure,
            controlsAvailable: !_config.StrictBetaMode);
    }

    private void HandleHerobrineCues()
    {
        while (_herobrine.TryDequeueCue(out HerobrineCue cue))
        {
            switch (cue.Kind)
            {
                case HerobrineCueKind.Manifest:
                    _mobSoundSystem.Handle(new MobEvent(MobEventKind.Ambient, MobType.Zombie, cue.Position, cue.Intensity));
                    break;
                case HerobrineCueKind.Vanish:
                    _mobSoundSystem.Handle(new MobEvent(MobEventKind.Hurt, MobType.Zombie, cue.Position, cue.Intensity));
                    break;
                case HerobrineCueKind.Footstep:
                    _soundSystem.PlayStep(BlockId.Grass, cue.Position);
                    break;
                case HerobrineCueKind.Whisper:
                    _mobSoundSystem.Handle(new MobEvent(MobEventKind.Ambient, MobType.Zombie, cue.Position, cue.Intensity));
                    break;
                case HerobrineCueKind.Flicker:
                    _soundSystem.PlayPlace(BlockId.Torch, cue.Position);
                    break;
            }
        }
    }

    private void HandleMobEvent(MobEvent mobEvent)
    {
        _mobSoundSystem.Handle(mobEvent);

        if (mobEvent.Kind == MobEventKind.Hurt)
        {
            _session.Renderer.EmitMobHurtParticles(
                mobEvent.Type,
                mobEvent.Position,
                Vector3.Zero,
                mobEvent.Elite,
                mobEvent.EliteVariant,
                mobEvent.Intensity);
            return;
        }

        if (mobEvent.Kind == MobEventKind.Attack)
        {
            _session.Renderer.EmitMobAttackParticles(
                mobEvent.Type,
                mobEvent.Position,
                Vector3.Zero,
                mobEvent.Elite,
                mobEvent.EliteVariant,
                mobEvent.Intensity);
            return;
        }

        if (mobEvent.Kind == MobEventKind.Explosion)
        {
            int affectedBlocks = Math.Max(0, (int)MathF.Round((mobEvent.Intensity - 1f) * 64f));
            EmitExplosionEffects(mobEvent.Position, affectedBlocks, mobEvent.Intensity);
            return;
        }

        if (mobEvent.Kind != MobEventKind.Death)
        {
            return;
        }

        _session.Renderer.EmitMobDeathParticles(
            mobEvent.Type,
            mobEvent.Position,
            Vector3.Zero,
            mobEvent.Elite,
            mobEvent.EliteVariant,
            mobEvent.Intensity);

        LootTable.RollMobDrops(
            mobEvent.Type,
            mobEvent.Elite,
            mobEvent.EliteVariant,
            _config.Mob.EliteDropMultiplier,
            _session.World.Seed,
            mobEvent.Position,
            _lootDrops);
        if (_lootDrops.Count == 0)
        {
            return;
        }

        string mobLabel = mobEvent.Elite
            ? $"{EliteMobVariantCatalog.Get(mobEvent.EliteVariant).Name.ToLowerInvariant()} {mobEvent.Type.ToString().ToLowerInvariant()}"
            : mobEvent.Type.ToString();
        if (TryGrantLootDrops(_lootDrops, $"mob {mobLabel}"))
        {
            Log.Info($"Loot collected from {mobLabel} at {mobEvent.Position.X:F1},{mobEvent.Position.Y:F1},{mobEvent.Position.Z:F1}.");
        }
    }

    private bool TryOpenChest(BlockCoord chestPosition)
    {
        var block = _session.World.GetBlock(chestPosition.X, chestPosition.Y, chestPosition.Z);
        if (block != BlockId.Chest)
        {
            return false;
        }

        var chunkCoord = WorldType.ToChunkCoord(chestPosition.X, chestPosition.Z);
        if (!_session.World.TryGetChunk(chunkCoord, out var chunk) || chunk == null)
        {
            return false;
        }

        var (localX, localZ) = WorldType.ToLocalCoord(chestPosition.X, chestPosition.Z);
        var biome = chunk.GetBiome(localX, localZ);
        var poiKind = PoiLootClassifier.Classify(chunk, localX, chestPosition.Y, localZ, _session.Settings.SeaLevel);
        LootTable.RollChestLoot(_session.World.Seed, chestPosition, biome, chestPosition.Y, poiKind, _lootDrops);
        if (_lootDrops.Count == 0)
        {
            return false;
        }

        if (!TryGrantLootDrops(_lootDrops, $"chest at {chestPosition.X},{chestPosition.Y},{chestPosition.Z}"))
        {
            Log.Warn($"Chest at {chestPosition.X},{chestPosition.Y},{chestPosition.Z} could not be looted because inventory is full.");
            return false;
        }

        _worldEditor.SetBlock(chestPosition.X, chestPosition.Y, chestPosition.Z, BlockId.Air);
        var particlePosition = new Vector3(chestPosition.X + 0.5f, chestPosition.Y + 0.5f, chestPosition.Z + 0.5f);
        _soundSystem.PlayPlace(BlockId.Chest, particlePosition);
        _session.Renderer.EmitBlockPlaceParticles(BlockId.Chest, particlePosition, Vector3.Zero);
        string poiSuffix = poiKind == PoiLootKind.Generic ? string.Empty : $" with {poiKind} loot";
        Log.Info($"Opened chest at {chestPosition.X},{chestPosition.Y},{chestPosition.Z} in biome {BiomeRegistry.Get(biome).Name}{poiSuffix}.");
        return true;
    }

    private bool TryGrantLootDrops(List<LootDrop> lootDrops, string sourceLabel)
    {
        bool addedAny = false;
        for (int i = 0; i < lootDrops.Count; i++)
        {
            LootDrop drop = lootDrops[i];
            if (_inventory.TryAddItem(drop.Item, drop.Count))
            {
                addedAny = true;
                continue;
            }

            Log.Warn($"Inventory full while collecting {sourceLabel}; lost {drop.Item}x{drop.Count}.");
        }

        if (addedAny)
        {
            UpdateInventoryLabels();
            UpdateInventoryItems();
        }

        lootDrops.Clear();
        return addedAny;
    }

    private void HandleTntEvent(TntEvent tntEvent)
    {
        switch (tntEvent.Kind)
        {
            case TntEventKind.Primed:
                _soundSystem.PlayFuse(tntEvent.Position);
                break;
            case TntEventKind.Explosion:
                _soundSystem.PlayExplosion(tntEvent.Intensity, tntEvent.Position);
                EmitExplosionEffects(tntEvent.Position, tntEvent.AffectedBlocks, tntEvent.Intensity);
                break;
        }
    }

    private void HandleFireEvent(FireEvent fireEvent)
    {
        _session.Renderer.EmitFireParticles(fireEvent.Kind, fireEvent.Position, fireEvent.Intensity);
        _soundSystem.PlayFire(fireEvent.Kind, fireEvent.Position, fireEvent.Intensity);
    }

    private void EmitExplosionEffects(Vector3 position, int affectedBlocks, float intensity)
    {
        _session.Renderer.EmitExplosionParticles(position, affectedBlocks, intensity);
        _session.FireSystem.IgniteExplosion(_session.World, _session.Editor, position, affectedBlocks, intensity);

        int bursts = Math.Clamp((int)MathF.Ceiling(affectedBlocks / 12f + intensity * 1.5f), 4, 12);
        for (int i = 0; i < bursts; i++)
        {
            float lateral = (i % 3 - 1) * 0.18f;
            float height = 0.12f + (i % 2) * 0.08f;
            float depth = ((i / 3) % 3 - 1) * 0.18f;
            float motionScale = 2.7f + intensity * 0.35f;
            var motion = new Vector3(lateral * motionScale, 1.65f + i * 0.12f + intensity * 0.12f, depth * motionScale);
            var debris = (i % 6) switch
            {
                0 => BlockId.Dirt,
                1 => BlockId.Stone,
                2 => BlockId.Wood,
                3 => BlockId.Leaves,
                4 => BlockId.Coal,
                _ => BlockId.Gunpowder
            };
            _session.Renderer.EmitBlockBreakParticles(debris, position + new Vector3(lateral, height, depth), motion);
        }
    }

    private void HandlePlayerDeath()
    {
        if (_player.Health > 0)
        {
            return;
        }

        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        _player.Respawn(_spawnPosition);
        _playerFeedback.Reset(_player);
        _playerAttackTimer = 0f;
        _firstPersonSwingTimer = 0f;
        _cameraMotion.Reset();
        _session.ChunkManager.Update(_player.Position, _config.ChunkRadius, _config.ChunkPreloadExtra);
        SyncCameraToPlayer();
        SyncAudioListener();
        Log.Info($"Player respawned at {_spawnPosition.X:F1},{_spawnPosition.Y:F1},{_spawnPosition.Z:F1}");
    }

    private void SyncCameraToPlayer()
    {
        _camera.Position = _player.Position + new Vector3(0, _config.Physics.EyeHeight, 0) + _cameraMotion.CurrentOffset;
        _camera.Yaw = _player.Yaw;
        _camera.Pitch = _player.Pitch;
        _camera.Fov = _config.FieldOfView;
        int width = Math.Max(1, _renderDevice.Window.Width);
        int height = Math.Max(1, _renderDevice.Window.Height);
        _camera.AspectRatio = width / (float)height;
    }

    private void UpdateCameraMotion(float dt, float sprintMultiplier)
    {
        _cameraMotion.Update(
            dt,
            _player.Yaw,
            _player.Velocity,
            _player.OnGround,
            _player.InWater || _player.InLava,
            _config.PlayerSpeed * sprintMultiplier,
            _config.CameraMotion);
    }

    private void SyncAudioListener()
    {
        var right = _camera.GetRight();
        _soundSystem.SetListener(_camera.Position, right);
        _mobSoundSystem.SetListener(_camera.Position, right);
    }

    private void StepBiomeMenu(int delta)
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

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
        if (_config.StrictBetaMode)
        {
            return;
        }

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
        if (_config.StrictBetaMode)
        {
            return string.Empty;
        }

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
        sb.AppendLine("Enter: regenerate  F6: seed  Esc/F5: close");
        return sb.ToString();
    }

    private string BuildSeedMenuText()
    {
        if (_config.StrictBetaMode)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Seed:");
        sb.AppendLine(string.IsNullOrWhiteSpace(_seedInput) ? "<empty>" : _seedInput);
        sb.AppendLine("Enter: apply  F6: random  Esc: close");
        sb.AppendLine("Tip: number or text");
        return sb.ToString();
    }

    private bool TryCraftInventory()
    {
        if (!_craftingSystem.TryCraft(_inventory, out var recipe) || recipe is null)
        {
            return false;
        }

        _itemNameTimer = _config.Ui.ItemNameDisplaySeconds;
        Log.Info($"Crafted {FormatActionText(recipe.Output, recipe.OutputCount)}.");
        return true;
    }

    private bool TrySmeltInventory()
    {
        if (!_smeltingSystem.TrySmelt(_inventory, out var recipe) || recipe is null)
        {
            return false;
        }

        _itemNameTimer = _config.Ui.ItemNameDisplaySeconds;
        Log.Info($"Smelted {FormatActionText(recipe.Output, recipe.OutputCount)}.");
        return true;
    }

    private bool TryRepairInventory()
    {
        if (!_toolRepairSystem.TryRepairSelected(_inventory, out var repairInfo))
        {
            SetStatusToast("No repair available.");
            return false;
        }

        _itemNameTimer = _config.Ui.ItemNameDisplaySeconds;
        UpdateInventoryLabels();
        UpdateInventoryItems();
        SetStatusToast(
            $"Repaired {FormatActionText(repairInfo.Tool, 1)} by {repairInfo.RepairAmount} durability using {FormatActionText(repairInfo.Material, repairInfo.MaterialCount)}.");
        Log.Info(
            $"Repaired {repairInfo.Tool} from {repairInfo.CurrentDurability} to {repairInfo.NewDurability} using {repairInfo.MaterialCount}x {repairInfo.Material}.");
        return true;
    }

    private bool TryRest()
    {
        if (!_config.Survival.Enabled || !_config.Survival.EnableRest)
        {
            return false;
        }

        if (_sleepSystem.TryRest(_player, _session.Mobs, _dayNight))
        {
            _playerAttackTimer = 0f;
            _firstPersonSwingTimer = 0f;
            _cameraMotion.Reset();
            SetStatusToast("You rest until morning.");
            Log.Info($"Player rested until morning at day {_dayNight.DayCount}.");
            return true;
        }

        SetStatusToast("Cannot rest right now.");
        return false;
    }

    private static string FormatActionText(BlockId item, int count)
    {
        string name = FormatBlockName(NormalizeBlockName(item));
        return count > 1 ? $"{name} x{count}" : name;
    }

    private void SetStatusToast(string text, float durationSeconds = 2.5f)
    {
        _statusToastText = text;
        _statusToastTimer = Math.Max(0.1f, durationSeconds);
    }

    private void RegenerateWorld(BiomeId? forcedBiome, int? newSeed)
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        _showBiomeMenu = false;
        _showSeedMenu = false;
        _showSettingsMenu = false;
        _showAudioSettingsMenu = false;
        _paused = false;
        _pendingRegen = false;

        int targetSeed = newSeed ?? _seed;
        string? targetBiome = forcedBiome?.ToString();

        _saveQueue.Flush();

        if (!WorldReset.TryReset(_config.WorldPath, targetSeed, out var resetError))
        {
            Log.Warn($"World regeneration failed: {resetError}");
            return;
        }

        _seed = targetSeed;
        _config.Seed = _seed;
        _config.WorldGen.ForcedBiome = targetBiome;

        _session.Dispose();
        _session = new WorldSession(_seed, _config, _assets, _renderDevice);
        _changeQueue = _session.ChangeQueue;
        _worldEditor = _session.Editor;
        _saveQueue = _session.SaveQueue;

        try
        {
            SaveConfig();
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save config after world regeneration: {ex.Message}");
        }

        var settings = _session.Settings;
        var bootstrapGenerator = new WorldGenerator(_seed, settings);
        Vector3 startPos;
        if (SpawnLocator.TryFindSpawn(_seed, settings, _config.Spawn, out var spawn, out var biome))
        {
            startPos = spawn;
            Log.Info($"Spawned in biome {BiomeRegistry.Get(biome).Name} at {startPos.X:F1},{startPos.Y:F1},{startPos.Z:F1}");
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

        _spawnPosition = startPos;
        _player = new Player(startPos, _config.Physics);
        _playerFeedback.Reset(_player);
        _cameraMotion.Reset();
        SyncCameraToPlayer();
        SyncAudioListener();
        LoadSavedMobs();

        _inventory.Reset();
        _hungerSystem.Reset();
        _statusToastText = string.Empty;
        _statusToastTimer = 0f;
        UpdateInventoryLabels();
        UpdateInventoryItems();
        _lastSelectedSlot = _inventory.SelectedIndex;
        _itemNameTimer = _config.Ui.ItemNameDisplaySeconds;
        _dayNight.Reset();
        _weather.Reset();
        _timeSeconds = 0f;
        _autoSaveTimer = 0f;
        _playerAttackTimer = 0f;
        _firstPersonSwingTimer = 0f;
        _ambientSoundSystem.Reset();
        _herobrine = new HerobrineSystem(_seed, _config.Herobrine);
        LoadSavedHerobrine();

        BuildBiomeMenuItems();
    }

    private void OpenSeedMenu()
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        _showSeedMenu = true;
        _showBiomeMenu = false;
        _seedInput = _seed.ToString(CultureInfo.InvariantCulture);
        SyncCursorState();
    }

    private void CloseSeedMenu()
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        _showSeedMenu = false;
        _seedInput = string.Empty;
        SyncCursorState();
    }

    private void HandleSeedMenuKey(KeyEvent key)
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        switch (key.Key)
        {
            case Key.Escape:
                CloseSeedMenu();
                return;
            case Key.Enter:
                QueueSeedApply();
                return;
            case Key.BackSpace:
                if (_seedInput.Length > 0)
                {
                    _seedInput = _seedInput[..^1];
                }
                return;
            case Key.F6:
                QueueRandomSeed();
                return;
        }

        if (TrySeedChar(key.Key, out char ch))
        {
            if (_seedInput.Length < 32)
            {
                _seedInput += ch;
            }
        }
    }

    private void QueueSeedApply()
    {
        int seed = ParseSeedInput(_seedInput);
        _pendingSeed = seed;
        _pendingRegenBiome = _session.Settings.ForcedBiome;
        _pendingRegen = true;
        _showSeedMenu = false;
    }

    private void QueueRandomSeed()
    {
        _pendingSeed = GenerateRandomSeed();
        _pendingRegenBiome = _session.Settings.ForcedBiome;
        _pendingRegen = true;
        _showSeedMenu = false;
    }

    private static int GenerateRandomSeed()
    {
        return Random.Shared.Next(int.MinValue, int.MaxValue);
    }

    private static int ParseSeedInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return GenerateRandomSeed();
        }

        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
        {
            return seed;
        }

        return HashSeedString(input);
    }

    private static int HashSeedString(string text)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }

    private static bool TrySeedChar(Key key, out char ch)
    {
        ch = default;
        switch (key)
        {
            case Key.Space: ch = ' '; return true;
            case Key.Minus: ch = '-'; return true;
            case Key.Number0: ch = '0'; return true;
            case Key.Number1: ch = '1'; return true;
            case Key.Number2: ch = '2'; return true;
            case Key.Number3: ch = '3'; return true;
            case Key.Number4: ch = '4'; return true;
            case Key.Number5: ch = '5'; return true;
            case Key.Number6: ch = '6'; return true;
            case Key.Number7: ch = '7'; return true;
            case Key.Number8: ch = '8'; return true;
            case Key.Number9: ch = '9'; return true;
            case Key.Keypad0: ch = '0'; return true;
            case Key.Keypad1: ch = '1'; return true;
            case Key.Keypad2: ch = '2'; return true;
            case Key.Keypad3: ch = '3'; return true;
            case Key.Keypad4: ch = '4'; return true;
            case Key.Keypad5: ch = '5'; return true;
            case Key.Keypad6: ch = '6'; return true;
            case Key.Keypad7: ch = '7'; return true;
            case Key.Keypad8: ch = '8'; return true;
            case Key.Keypad9: ch = '9'; return true;
            case Key.A: ch = 'a'; return true;
            case Key.B: ch = 'b'; return true;
            case Key.C: ch = 'c'; return true;
            case Key.D: ch = 'd'; return true;
            case Key.E: ch = 'e'; return true;
            case Key.F: ch = 'f'; return true;
            case Key.G: ch = 'g'; return true;
            case Key.H: ch = 'h'; return true;
            case Key.I: ch = 'i'; return true;
            case Key.J: ch = 'j'; return true;
            case Key.K: ch = 'k'; return true;
            case Key.L: ch = 'l'; return true;
            case Key.M: ch = 'm'; return true;
            case Key.N: ch = 'n'; return true;
            case Key.O: ch = 'o'; return true;
            case Key.P: ch = 'p'; return true;
            case Key.Q: ch = 'q'; return true;
            case Key.R: ch = 'r'; return true;
            case Key.S: ch = 's'; return true;
            case Key.T: ch = 't'; return true;
            case Key.U: ch = 'u'; return true;
            case Key.V: ch = 'v'; return true;
            case Key.W: ch = 'w'; return true;
            case Key.X: ch = 'x'; return true;
            case Key.Y: ch = 'y'; return true;
            case Key.Z: ch = 'z'; return true;
            default:
                return false;
        }
    }

    private void TogglePause()
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        _showSettingsMenu = false;
        _showAudioSettingsMenu = false;
        _paused = !_paused;
        if (_paused)
        {
            _pauseMenuIndex = 0;
        }

        SyncCursorState();
    }

    private void OpenSettingsMenu()
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        _paused = true;
        _showSettingsMenu = true;
        _showAudioSettingsMenu = false;
        _settingsMenuIndex = 0;
        SyncCursorState();
    }

    private void CloseSettingsMenu()
    {
        _showSettingsMenu = false;
        _showAudioSettingsMenu = false;
        SyncCursorState();
    }

    private void OpenAudioSettingsMenu()
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        _paused = true;
        _showSettingsMenu = true;
        _showAudioSettingsMenu = true;
        _audioSettingsMenuIndex = 0;
        SyncCursorState();
    }

    private void CloseAudioSettingsMenu()
    {
        _showAudioSettingsMenu = false;
        SyncCursorState();
    }

    private void HandlePauseMenuKey(KeyEvent key)
    {
        if (_config.StrictBetaMode)
        {
            _paused = false;
            SyncCursorState();
            return;
        }

        switch (key.Key)
        {
            case Key.Up:
            case Key.Left:
                StepPauseMenu(-1);
                break;
            case Key.Down:
            case Key.Right:
                StepPauseMenu(1);
                break;
            case Key.Enter:
            case Key.Space:
                ActivatePauseMenuSelection();
                break;
            case Key.Escape:
            case Key.Tab:
                TogglePause();
                break;
            case Key.F3:
                ToggleDebugHud();
                break;
            case Key.F11:
                ToggleFullscreen();
                break;
        }
    }

    private void HandleSettingsMenuKey(KeyEvent key)
    {
        if (_config.StrictBetaMode)
        {
            _showSettingsMenu = false;
            _showAudioSettingsMenu = false;
            SyncCursorState();
            return;
        }

        switch (key.Key)
        {
            case Key.Up:
                StepSettingsMenu(-1);
                break;
            case Key.Down:
                StepSettingsMenu(1);
                break;
            case Key.Left:
                AdjustSelectedSetting(-1);
                break;
            case Key.Right:
                AdjustSelectedSetting(1);
                break;
            case Key.Enter:
            case Key.Space:
                ActivateSettingsMenuSelection();
                break;
            case Key.Escape:
                CloseSettingsMenu();
                break;
            case Key.Tab:
                TogglePause();
                break;
            case Key.F3:
                ToggleDebugHud();
                break;
            case Key.F11:
                ToggleFullscreen();
                break;
        }
    }

    private void HandleAudioSettingsMenuKey(KeyEvent key)
    {
        if (_config.StrictBetaMode)
        {
            _showAudioSettingsMenu = false;
            _showSettingsMenu = false;
            SyncCursorState();
            return;
        }

        switch (key.Key)
        {
            case Key.Up:
                StepAudioSettingsMenu(-1);
                break;
            case Key.Down:
                StepAudioSettingsMenu(1);
                break;
            case Key.Left:
                AdjustSelectedAudioSetting(-1);
                break;
            case Key.Right:
                AdjustSelectedAudioSetting(1);
                break;
            case Key.Enter:
            case Key.Space:
                ActivateAudioSettingsMenuSelection();
                break;
            case Key.Escape:
                CloseAudioSettingsMenu();
                break;
            case Key.Tab:
                TogglePause();
                break;
            case Key.F3:
                ToggleDebugHud();
                break;
            case Key.F11:
                ToggleFullscreen();
                break;
        }
    }

    private void ActivatePauseMenuSelection()
    {
        switch (_pauseMenuIndex)
        {
            case 0:
                TogglePause();
                break;
            case 1:
                OpenSettingsMenu();
                break;
            case 2:
                _exitRequested = true;
                break;
        }
    }

    private void ActivateSettingsMenuSelection()
    {
        switch (_settingsMenuIndex)
        {
            case 0:
                ToggleFullscreen();
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
                ToggleDebugHud();
                break;
            case 4:
                OpenAudioSettingsMenu();
                break;
            case 5:
                CloseSettingsMenu();
                break;
        }
    }

    private void ActivateAudioSettingsMenuSelection()
    {
        if (_audioSettingsMenuIndex == 5)
        {
            CloseAudioSettingsMenu();
        }
    }

    private void AdjustSelectedSetting(int delta)
    {
        switch (_settingsMenuIndex)
        {
            case 0:
                ToggleFullscreen();
                break;
            case 1:
                AdjustFieldOfView(delta * FieldOfViewStep);
                break;
            case 2:
                AdjustMouseSensitivity(delta * MouseSensitivityStep);
                break;
            case 3:
                ToggleDebugHud();
                break;
            case 4:
                OpenAudioSettingsMenu();
                break;
            case 5:
                CloseSettingsMenu();
                break;
        }
    }

    private void AdjustSelectedAudioSetting(int delta)
    {
        switch (_audioSettingsMenuIndex)
        {
            case 0:
                AdjustAudioVolume("Master volume", _config.Audio.MasterVolume, value => _config.Audio.MasterVolume = value, delta * AudioVolumeStep);
                break;
            case 1:
                AdjustAudioVolume("Music volume", _config.Audio.MusicVolume, value => _config.Audio.MusicVolume = value, delta * AudioVolumeStep);
                break;
            case 2:
                AdjustAudioVolume("Ambient volume", _config.Audio.AmbientVolume, value => _config.Audio.AmbientVolume = value, delta * AudioVolumeStep);
                break;
            case 3:
                AdjustAudioVolume("Weather volume", _config.Audio.WeatherVolume, value => _config.Audio.WeatherVolume = value, delta * AudioVolumeStep);
                break;
            case 4:
                AdjustAudioVolume("Mob volume", _config.Audio.MobVolume, value => _config.Audio.MobVolume = value, delta * AudioVolumeStep);
                break;
            case 5:
                CloseAudioSettingsMenu();
                break;
        }
    }

    private void StepPauseMenu(int delta)
    {
        _pauseMenuIndex = StepMenuIndex(_pauseMenuIndex, 3, delta);
    }

    private void StepSettingsMenu(int delta)
    {
        _settingsMenuIndex = StepMenuIndex(_settingsMenuIndex, 6, delta);
    }

    private void StepAudioSettingsMenu(int delta)
    {
        _audioSettingsMenuIndex = StepMenuIndex(_audioSettingsMenuIndex, 6, delta);
    }

    private static int StepMenuIndex(int index, int count, int delta)
    {
        if (count <= 0)
        {
            return 0;
        }

        int next = (index + delta) % count;
        if (next < 0)
        {
            next += count;
        }

        return next;
    }

    private void ToggleHerobrine()
    {
        _config.Herobrine.Enabled = !_config.Herobrine.Enabled;
        if (!_config.Herobrine.Enabled)
        {
            _herobrine.Disable(_session.World, _session.Editor, _session.Mobs);
        }

        _herobrineToastText = HerobrineHudText.BuildToastText(_config.Herobrine.Enabled);
        _herobrineToastTimer = 2f;
        SaveConfig();
    }

    private void CycleHerobrineMode()
    {
        _config.Herobrine.Mode = HerobrineModeCatalog.Next(_config.Herobrine.Mode);
        var modeProfile = HerobrineModeCatalog.Get(_config.Herobrine.Mode);
        _herobrineToastText = HerobrineHudText.BuildModeToastText(modeProfile.Name, modeProfile.Description);
        _herobrineToastTimer = 2.5f;
        SaveConfig();
    }

    private void SyncCursorState()
    {
        bool uiOpen = _paused || _showSettingsMenu || _showAudioSettingsMenu || _showBiomeMenu || _showSeedMenu || _inventoryOpen;
        _renderDevice.Window.CursorVisible = uiOpen;
        SetMouseMode(captured: !uiOpen, relative: !uiOpen);
        _inputHandler.SetRelative(!uiOpen);
        _input.ResetDeltas();
        _inputHandler.ResetMouse();
    }

    private void ToggleFullscreen()
    {
        var window = _renderDevice.Window;
        bool fullscreen = window.WindowState == WindowState.BorderlessFullScreen || window.WindowState == WindowState.FullScreen;
        if (fullscreen)
        {
            window.WindowState = WindowState.Normal;
        }
        else
        {
            window.WindowState = WindowState.BorderlessFullScreen;
        }

        SetStatusToast(fullscreen ? "Fullscreen disabled" : "Fullscreen enabled", 1.5f);
        SaveConfig();
    }

    private void ToggleDebugHud()
    {
        if (_config.StrictBetaMode)
        {
            return;
        }

        _showDebug = !_showDebug;
        _config.ShowDebug = _showDebug;
        SetStatusToast(_showDebug ? "Debug HUD enabled" : "Debug HUD disabled", 1.5f);
        SaveConfig();
    }

    private void AdjustFieldOfView(float delta)
    {
        float next = Math.Clamp(_config.FieldOfView + delta, GameConfig.MinFieldOfView, GameConfig.MaxFieldOfView);
        if (Math.Abs(next - _config.FieldOfView) < 0.001f)
        {
            return;
        }

        _config.FieldOfView = next;
        SetStatusToast($"Field of view: {next:0} deg", 1.5f);
        SaveConfig();
    }

    private void AdjustMouseSensitivity(float delta)
    {
        float next = Math.Clamp(_config.MouseSensitivity + delta, GameConfig.MinMouseSensitivity, GameConfig.MaxMouseSensitivity);
        if (Math.Abs(next - _config.MouseSensitivity) < 0.0001f)
        {
            return;
        }

        _config.MouseSensitivity = next;
        SetStatusToast($"Mouse sensitivity: {next:0.00}", 1.5f);
        SaveConfig();
    }

    private void AdjustAudioVolume(string label, float currentValue, Action<float> applyValue, float delta)
    {
        float next = Math.Clamp(currentValue + delta, 0f, 1f);
        if (Math.Abs(next - currentValue) < 0.0001f)
        {
            return;
        }

        applyValue(next);
        SetStatusToast($"{label}: {FormatAudioVolume(next)}", 1.5f);
        SaveConfig();
    }

    private static string FormatAudioVolume(float value)
    {
        int percent = (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 100f);
        return percent.ToString(CultureInfo.InvariantCulture) + "%";
    }

    private void SetMouseMode(bool captured, bool relative)
    {
        var handle = _renderDevice.Window.SdlWindowHandle;
        Sdl2Native.SDL_SetWindowGrab(handle, captured);
        Sdl2Native.SDL_SetRelativeMouseMode(relative);
    }

    private void HandleBlockInteraction(float dt)
    {
        var forward = _camera.GetForward();
        var result = WorldRaycaster.Raycast(_session.World, _camera.Position, forward, 6f);
        var mobResult = _session.Mobs.Raycast(_camera.Position, forward, 6f);
        bool mobFirst = mobResult.Hit && (!result.Hit || mobResult.Distance <= result.Distance);
        UpdateSelectionState(result, mobResult);

        if (_input.MouseLeft)
        {
            if (mobFirst)
            {
                if (!_leftHandled)
                {
                    TriggerFirstPersonSwing();
                    TryAttackMob(mobResult);
                    _leftHandled = true;
                }

                _blockBreakSystem.Cancel();
            }
            else if (result.Hit)
            {
                BlockCoord target = result.Block;
                BlockId selectedTool = _inventory.GetSelectedBlock();
                bool sameBreak = _blockBreakSystem.IsActive &&
                                 _blockBreakSystem.Target.Equals(target) &&
                                 _blockBreakSystem.Tool == selectedTool;

                if (!sameBreak)
                {
                    _blockBreakSystem.Cancel();
                    if (TryStartBlockBreak(target, selectedTool))
                    {
                        if (!_leftHandled)
                        {
                            TriggerFirstPersonSwing();
                        }
                        _leftHandled = true;
                    }
                }
                else if (_blockBreakSystem.Update(dt))
                {
                    CompleteBlockBreak();
                }
            }
            else
            {
                _blockBreakSystem.Cancel();
            }
        }
        else
        {
            _leftHandled = false;
            _blockBreakSystem.Cancel();
        }

        if (_input.MouseRight && !_rightHandled)
        {
            TriggerFirstPersonSwing();
            if (result.Hit)
            {
                var targetBlock = _session.World.GetBlock(result.Block.X, result.Block.Y, result.Block.Z);
                if (targetBlock == BlockId.Chest)
                {
                    TryOpenChest(result.Block);
                    _rightHandled = true;
                    return;
                }

                if (_config.Survival.Enabled && TryConsumeSelectedFood())
                {
                    _rightHandled = true;
                    return;
                }

                var block = _inventory.GetSelectedBlock();
                if (!BlockRegistry.Get(block).IsPlaceable)
                {
                    _rightHandled = true;
                    return;
                }
                if (block == BlockId.Torch)
                {
                    block = ResolveTorchPlacement(result.Block, result.Adjacent);
                }
                if (_session.World.GetBlock(result.Adjacent.X, result.Adjacent.Y, result.Adjacent.Z) == BlockId.Air &&
                    !_player.IntersectsBlock(result.Adjacent))
                {
                    if (_inventory.TryConsumeSelected(1))
                    {
                        var particlePosition = new Vector3(result.Adjacent.X + 0.5f, result.Adjacent.Y + 0.5f, result.Adjacent.Z + 0.5f);
                        _worldEditor.SetBlock(result.Adjacent.X, result.Adjacent.Y, result.Adjacent.Z, block);
                        if (block == BlockId.Tnt && _config.Tnt.PrimeOnPlace)
                        {
                            _session.TntSystem.Prime(_session.World, result.Adjacent, _config.Tnt.FuseSeconds);
                        }
                        _soundSystem.PlayPlace(block, particlePosition);
                        _session.Renderer.EmitBlockPlaceParticles(block, particlePosition, Vector3.Zero);
                    }
                }
            }
            _rightHandled = true;
        }
        if (!_input.MouseRight)
        {
            _rightHandled = false;
        }
    }

    private bool TryConsumeSelectedFood()
    {
        BlockId selected = _inventory.GetSelectedBlock();
        if (!FoodCatalog.TryGetHungerRestore(selected, out _))
        {
            return false;
        }

        if (_hungerSystem.Hunger >= _hungerSystem.MaxHunger)
        {
            SetStatusToast("Hunger is already full.");
            return true;
        }

        if (!_inventory.TryConsumeSelected(1))
        {
            SetStatusToast("Failed to consume the selected food.");
            return true;
        }

        if (!_hungerSystem.TryEat(selected, out int hungerRestored))
        {
            _inventory.TryAddItem(selected, 1);
            SetStatusToast($"Can't eat {FormatBlockName(NormalizeBlockName(selected))} right now.");
            return true;
        }

        _itemNameTimer = _config.Ui.ItemNameDisplaySeconds;
        UpdateInventoryLabels();
        UpdateInventoryItems();
        SetStatusToast($"Ate {FormatActionText(selected, 1)} and restored {hungerRestored} hunger.");
        return true;
    }

    private bool TryStartBlockBreak(BlockCoord target, BlockId tool)
    {
        var block = _session.World.GetBlock(target.X, target.Y, target.Z);
        if (block == BlockId.Air)
        {
            return false;
        }

        if (!_harvestSystem.TryHarvest(block, tool, out var drop, out int dropCount, out bool toolUsed, out float breakSeconds))
        {
            drop = BlockId.Air;
            dropCount = 0;
        }

        if (!_blockBreakSystem.TryStart(target, block, tool, drop, dropCount, toolUsed, breakSeconds))
        {
            return false;
        }

        return true;
    }

    private void CompleteBlockBreak()
    {
        BlockCoord target = _blockBreakSystem.Target;
        BlockId block = _blockBreakSystem.Block;
        BlockId drop = _blockBreakSystem.Drop;
        int dropCount = _blockBreakSystem.Count;
        bool toolUsed = _blockBreakSystem.ToolUsed;
        BlockId tool = _blockBreakSystem.Tool;
        var particlePosition = new Vector3(target.X + 0.5f, target.Y + 0.5f, target.Z + 0.5f);

        _worldEditor.SetBlock(target.X, target.Y, target.Z, BlockId.Air);
        if (drop != BlockId.Air && dropCount > 0)
        {
            if (!_inventory.TryAddItem(drop, dropCount))
            {
                Log.Warn($"Inventory full while harvesting {block}; drop {drop}x{dropCount} was lost.");
            }
        }

        if (toolUsed && ToolCatalog.IsTool(tool) && _inventory.GetSelectedBlock() == tool &&
            _inventory.TryDamageSelectedTool(_config.Tools.ToolWearPerAction, out bool broke) && broke)
        {
            SetStatusToast($"{FormatBlockName(NormalizeBlockName(tool))} broke.");
            UpdateInventoryLabels();
            UpdateInventoryItems();
        }

        _soundSystem.PlayDig(block, particlePosition);
        _session.Renderer.EmitBlockBreakParticles(block, particlePosition, Vector3.Zero);
        _blockBreakSystem.Cancel();
    }

    private void TryAttackMob(MobHitResult mobResult)
    {
        var selectedTool = _inventory.GetSelectedBlock();
        int attackDamage = Math.Max(1, _config.Mob.PlayerAttackDamage + ToolCatalog.GetCombatBonus(selectedTool));
        if (_playerAttackTimer <= 0f && _session.Mobs.TryDamageMob(mobResult, attackDamage, _player.Position))
        {
            _playerAttackTimer = _config.Mob.PlayerAttackCooldownSeconds;
            if (ToolCatalog.IsTool(selectedTool) && _inventory.TryDamageSelectedTool(_config.Tools.ToolWearPerAction, out bool broke) && broke)
            {
                SetStatusToast($"{FormatBlockName(NormalizeBlockName(selectedTool))} broke.");
                UpdateInventoryLabels();
                UpdateInventoryItems();
            }
        }
    }

    private void UpdateSelectionState(RaycastResult result, MobHitResult mobResult)
    {
        if (mobResult.Hit && (!result.Hit || mobResult.Distance <= result.Distance))
        {
            if (mobResult.Index >= 0 && mobResult.Index < _session.Mobs.RenderInstances.Count)
            {
                _selectionState = SelectionState.ForMob(_session.Mobs.RenderInstances[mobResult.Index], mobResult.Distance);
                return;
            }
        }

        if (result.Hit)
        {
            float progress = _blockBreakSystem.IsActive && _blockBreakSystem.Target.Equals(result.Block) ? _blockBreakSystem.Progress : 0f;
            _selectionState = SelectionState.ForBlock(result.Block, result.Distance, progress);
            return;
        }

        _selectionState = SelectionState.None;
    }

    private void TriggerFirstPersonSwing()
    {
        _firstPersonSwingTimer = _config.FirstPerson.SwingDurationSeconds;
    }

    private FirstPersonRenderState BuildFirstPersonRenderState()
    {
        bool visible = !_paused && !_showSettingsMenu && !_showAudioSettingsMenu && !_showBiomeMenu && !_showSeedMenu && !_inventoryOpen;
        BlockId heldBlock = _inventory.GetSelectedBlock();
        float duration = Math.Max(0.01f, _config.FirstPerson.SwingDurationSeconds);
        float swingProgress = _firstPersonSwingTimer <= 0f ? 0f : 1f - Math.Clamp(_firstPersonSwingTimer / duration, 0f, 1f);
        float horizontalSpeed = new Vector2(_player.Velocity.X, _player.Velocity.Z).Length();
        float sprintMultiplier = _hungerSystem.CanSprint ? _config.Physics.SprintMultiplier : 1f;
        float movementDenominator = Math.Max(0.01f, _config.PlayerSpeed * sprintMultiplier);
        float movementStrength = Math.Clamp(horizontalSpeed / movementDenominator, 0f, 1f);

        return new FirstPersonRenderState(visible, heldBlock, swingProgress, movementStrength, _timeSeconds, _player.OnGround);
    }

    public void Dispose()
    {
        PersistShutdownState();
        _session.Dispose();
        _soundSystem.Dispose();
        _ambientSoundSystem.Dispose();
        _mobSoundSystem.Dispose();
        _audioBackend.Dispose();
        _renderDevice.Dispose();
    }
}
