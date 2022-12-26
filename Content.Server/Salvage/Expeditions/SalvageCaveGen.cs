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

    /// <summary>
    /// How wide the exterior border is, to prevent players wandering off.
    /// </summary>
    [DataField("borderWidth")]
    public int BorderWidth = 4;
}
