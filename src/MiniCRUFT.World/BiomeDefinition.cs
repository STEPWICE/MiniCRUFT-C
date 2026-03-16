namespace MiniCRUFT.World;

public sealed class BiomeDefinition
{
    public BiomeId Id { get; }
    public string Name { get; }
    public float Temperature { get; }
    public float Humidity { get; }
    public BlockId SurfaceBlock { get; }

    public BiomeDefinition(BiomeId id, string name, float temperature, float humidity, BlockId surfaceBlock)
    {
        Id = id;
        Name = name;
        Temperature = temperature;
        Humidity = humidity;
        SurfaceBlock = surfaceBlock;
    }
}
