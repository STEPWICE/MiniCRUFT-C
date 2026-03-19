namespace MiniCRUFT.World;

public static class ExplosionResistance
{
    public static float Get(BlockId id)
    {
        return id switch
        {
            BlockId.Air => 0f,
            BlockId.Grass => 0.6f,
            BlockId.Dirt => 0.6f,
            BlockId.Stone => 6f,
            BlockId.Sand => 0.6f,
            BlockId.Gravel => 0.5f,
            BlockId.Wood => 2f,
            BlockId.Leaves => 0.25f,
            BlockId.Water => 0.8f,
            BlockId.CoalOre => 6f,
            BlockId.IronOre => 6f,
            BlockId.GoldOre => 6f,
            BlockId.DiamondOre => 6f,
            BlockId.Bedrock => float.PositiveInfinity,
            BlockId.Planks => 2f,
            BlockId.Glass => 0.3f,
            BlockId.Torch => 0.05f,
            BlockId.Flower => 0.05f,
            BlockId.TallGrass => 0.05f,
            BlockId.Snow => 0.2f,
            BlockId.Clay => 0.6f,
            BlockId.BirchWood => 2f,
            BlockId.BirchLeaves => 0.25f,
            BlockId.SpruceWood => 2f,
            BlockId.SpruceLeaves => 0.25f,
            BlockId.Cactus => 0.4f,
            BlockId.DeadBush => 0.05f,
            BlockId.SugarCane => 0.05f,
            BlockId.Fire => 0f,
            BlockId.Water1 => 0.8f,
            BlockId.Water2 => 0.8f,
            BlockId.Water3 => 0.8f,
            BlockId.Water4 => 0.8f,
            BlockId.Water5 => 0.8f,
            BlockId.Water6 => 0.8f,
            BlockId.Water7 => 0.8f,
            BlockId.Cobblestone => 6f,
            BlockId.Obsidian => 1200f,
            BlockId.Lava => 3f,
            BlockId.Lava1 => 3f,
            BlockId.Lava2 => 3f,
            BlockId.Lava3 => 3f,
            BlockId.Lava4 => 3f,
            BlockId.Lava5 => 3f,
            BlockId.Lava6 => 3f,
            BlockId.Lava7 => 3f,
            BlockId.Tnt => 0.5f,
            _ => 1.5f
        };
    }
}
