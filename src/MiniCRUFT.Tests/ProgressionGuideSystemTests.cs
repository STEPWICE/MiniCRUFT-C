using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Tests;

public sealed class ProgressionGuideSystemTests
{
    [Fact]
    public void BuildGuideText_PrioritizesHungerWhenFoodIsNeeded()
    {
        var context = CreateContext(
            strictBetaMode: false,
            survivalEnabled: true,
            hunger: 4f,
            inventory: new Inventory(strictBetaMode: true, new ToolConfig()),
            playerPosition: new Vector3(8.5f, 65f, 8.5f));
        Assert.True(context.Inventory.TryAddItem(BlockId.Apple, 1));

        string text = new ProgressionGuideSystem().BuildGuideText(context);

        Assert.Contains("eat food", text, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGuideText_ShowsCraftingStep_WhenCraftableProgressionExists()
    {
        var context = CreateContext(
            strictBetaMode: false,
            survivalEnabled: false,
            playerPosition: new Vector3(8.5f, 65f, 8.5f));
        context.Inventory.Reset();

        string text = new ProgressionGuideSystem().BuildGuideText(context);

        Assert.Contains("press C", text, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("crafting table", text, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGuideText_ShowsSmeltingStep_WhenCraftingIsNotAvailable()
    {
        var context = CreateContext(
            strictBetaMode: false,
            survivalEnabled: false,
            playerPosition: new Vector3(8.5f, 65f, 8.5f),
            inventory: new Inventory(strictBetaMode: true, new ToolConfig()));
        Assert.True(context.Inventory.TryAddItem(BlockId.RawIron, 1));
        Assert.True(context.Inventory.TryAddItem(BlockId.Coal, 1));

        string text = new ProgressionGuideSystem().BuildGuideText(context);

        Assert.Contains("press V", text, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("iron ingot", text, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGuideText_ShowsRepairHint_WhenSelectedToolIsDamaged()
    {
        var context = CreateContext(
            strictBetaMode: false,
            survivalEnabled: false,
            playerPosition: new Vector3(8.5f, 65f, 8.5f),
            inventory: new Inventory(strictBetaMode: true, new ToolConfig()));
        Assert.True(context.Inventory.TryAddItem(BlockId.StonePickaxe, 1));
        Assert.True(context.Inventory.TryDamageSelectedTool(9, out _));
        Assert.True(context.Inventory.TryAddItem(BlockId.Cobblestone, 1));

        string text = new ProgressionGuideSystem().BuildGuideText(context);

        Assert.Contains("repair", text, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cobblestone", text, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGuideText_FallsBackToExploration_WhenNoImmediateProcedureExists()
    {
        var context = CreateContext(
            strictBetaMode: false,
            survivalEnabled: false,
            inventory: new Inventory(strictBetaMode: true, new ToolConfig()),
            playerPosition: new Vector3(8.5f, 65f, 8.5f));

        string text = new ProgressionGuideSystem().BuildGuideText(context);

        Assert.Contains("explore caves", text, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildMilestoneText_ShowsTheCurrentRouteStep()
    {
        var context = CreateContext(
            strictBetaMode: false,
            survivalEnabled: false,
            playerPosition: new Vector3(8.5f, 65f, 8.5f));

        string text = new ProgressionMilestoneSystem().BuildMilestoneText(context);

        Assert.Contains("Milestones:", text, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[>]", text, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Craft a table", text, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildMilestoneText_ReachesRouteComplete_WhenCaveLoadoutIsReady()
    {
        var inventory = new Inventory(strictBetaMode: true, new ToolConfig());
        Assert.True(inventory.TryAddItem(BlockId.Wood, 1));
        Assert.True(inventory.TryAddItem(BlockId.CraftingTable, 1));
        Assert.True(inventory.TryAddItem(BlockId.Furnace, 1));
        Assert.True(inventory.TryAddItem(BlockId.Torch, 4));
        Assert.True(inventory.TryAddItem(BlockId.StonePickaxe, 1));
        Assert.True(inventory.TryAddItem(BlockId.IronIngot, 1));
        Assert.True(inventory.TryAddItem(BlockId.Bread, 1));

        var context = CreateContext(
            strictBetaMode: false,
            survivalEnabled: false,
            inventory: inventory,
            playerPosition: new Vector3(8.5f, 65f, 8.5f));

        string text = new ProgressionMilestoneSystem().BuildMilestoneText(context);

        Assert.Contains("Route complete", text, System.StringComparison.OrdinalIgnoreCase);
    }

    private static ProgressionGuideContext CreateContext(
        bool strictBetaMode,
        bool survivalEnabled,
        float hunger = 20f,
        Inventory? inventory = null,
        Vector3? playerPosition = null)
    {
        var survival = new SurvivalConfig
        {
            Enabled = survivalEnabled,
            MaxHunger = 20,
            StartingHunger = hunger,
            BaseDrainPerSecond = 1f,
            SprintDrainMultiplier = 2f,
            LiquidDrainMultiplier = 1f,
            MinHungerToSprint = 4,
            EnableRest = true,
            RestMinSunIntensity = 0.12f,
            RestWakeTimeOfDay = 0.25f,
            RestThreatRadius = 16f,
            StarvationDamageIntervalSeconds = 1f,
            StarvationDamage = 1
        };

        var config = new GameConfig
        {
            StrictBetaMode = strictBetaMode,
            Survival = survival,
            Tools = new ToolConfig()
        };

        var inv = inventory ?? new Inventory(strictBetaMode, config.Tools);
        if (inventory is null)
        {
            inv.Reset();
        }

        var player = new Player(playerPosition ?? new Vector3(8.5f, 65f, 8.5f), new PhysicsConfig());
        player.OnGround = true;
        player.Velocity = Vector3.Zero;

        var mobs = new MobSystem(1337, new MobConfig { Enabled = false });
        var dayNight = new DayNightCycle(new DayNightConfig { StartTimeOfDay = 0.75f });
        dayNight.SetState(0.75f, 3);

        var hungerSystem = new HungerSystem(survival);
        hungerSystem.Load(new HungerSaveData(hunger, 0f));

        return new ProgressionGuideContext(
            player,
            inv,
            hungerSystem,
            new SleepSystem(survival),
            dayNight,
            mobs,
            new CraftingSystem(),
            new SmeltingSystem(),
            new ToolRepairSystem(config.Tools),
            survival,
            strictBetaMode);
    }
}
