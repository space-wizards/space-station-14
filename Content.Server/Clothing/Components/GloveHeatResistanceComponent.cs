namespace Content.Server.Clothing.Components;

/// <summary>
///     TODO this needs removed somehow.
///     Handles 'heat resistance' for gloves touching bulbs and that's it, ick.
/// </summary>
[RegisterComponent]
public sealed partial class GloveHeatResistanceComponent : Component
{
    [DataField("heatResistance")]
    public int HeatResistance = 323;
}
