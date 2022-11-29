namespace Content.Server.Salvage.Expeditions;

[DataDefinition]
public sealed class SalvageCaveGen : ISalvageProcgen
{
    /// <summary>
    /// Tile-width
    /// </summary>
    [DataField("width")]
    public int Width = 128;

    /// <summary>
    /// Tile-height
    /// </summary>
    [DataField("height")]
    public int Height = 96;

    [DataField("steps")]
    public int Steps = 2;

    [DataField("solidChance")]
    public float SolidChance = 0.4f;
}
