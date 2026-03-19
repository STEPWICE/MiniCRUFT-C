using System;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class HungerSystem
{
    private readonly SurvivalConfig _config;
    private float _hunger;
    private float _starvationTimer;

    public HungerSystem(SurvivalConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        Reset();
    }

    public float Hunger => _hunger;

    public int MaxHunger => Math.Max(1, _config.MaxHunger);

    public bool CanSprint => !_config.Enabled || _hunger > _config.MinHungerToSprint;

    public void Reset()
    {
        _hunger = Math.Clamp(_config.StartingHunger, 0f, MaxHunger);
        _starvationTimer = 0f;
    }

    public void Load(HungerSaveData data)
    {
        _hunger = Math.Clamp(data.Hunger, 0f, MaxHunger);
        _starvationTimer = Math.Max(0f, data.StarvationTimer);
    }

    public HungerSaveData BuildSaveData()
    {
        return new HungerSaveData(_hunger, _starvationTimer);
    }

    public bool TryEat(BlockId item, out int hungerRestored)
    {
        hungerRestored = 0;
        if (!_config.Enabled || !FoodCatalog.TryGetHungerRestore(item, out int restore) || restore <= 0)
        {
            return false;
        }

        if (_hunger >= MaxHunger)
        {
            return false;
        }

        float before = _hunger;
        _hunger = Math.Min(MaxHunger, _hunger + restore);
        _starvationTimer = 0f;
        hungerRestored = (int)MathF.Ceiling(_hunger - before);
        return hungerRestored > 0;
    }

    public void Update(float dt, Player player, bool sprinting, bool inLiquid)
    {
        if (dt <= 0f || player is null)
        {
            return;
        }

        if (!_config.Enabled)
        {
            _hunger = MaxHunger;
            _starvationTimer = 0f;
            return;
        }

        float drain = Math.Max(0f, _config.BaseDrainPerSecond);
        if (sprinting)
        {
            drain *= Math.Max(1f, _config.SprintDrainMultiplier);
        }

        if (inLiquid)
        {
            drain *= Math.Max(1f, _config.LiquidDrainMultiplier);
        }

        if (drain > 0f)
        {
            _hunger = Math.Max(0f, _hunger - drain * dt);
        }

        if (_hunger > 0f)
        {
            _starvationTimer = 0f;
            return;
        }

        float starvationInterval = Math.Max(0.1f, _config.StarvationDamageIntervalSeconds);
        _starvationTimer += dt;
        while (_starvationTimer >= starvationInterval && player.Health > 0)
        {
            _starvationTimer -= starvationInterval;
            player.TryApplyDamage(Math.Max(1, _config.StarvationDamage), Vector3.Zero, 0f);
        }
    }
}
