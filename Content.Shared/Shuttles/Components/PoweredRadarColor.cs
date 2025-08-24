namespace Content.Shared.Shuttles.Components;

[RegisterComponent]
public sealed partial class PoweredRadarColorComponent : Component
{
    /// <summary>
    /// The radar signature color when powered
    /// </summary>
    [DataField]
    public Color OnColor = Color.FromHex("#00FF00");

    /// <summary>
    /// The radar signature color when unpowered
    /// </summary>
    [DataField]
    public Color OffColor = Color.FromHex("#FF0000");
}
