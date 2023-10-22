
namespace Content.Server.Tesla.Components;

/// <summary>
/// Generates electricity from lightning bolts
/// </summary>
[RegisterComponent]
public sealed partial class TeslaCoilComponent : Component
{
    /// <summary>
    /// How much power will the coil generate from a lightning strike
    /// </summary>
    // To Do: Different lightning bolts have different powers and generate different amounts of energy
    [DataField]
    public float ChargeFromLightning = 30000f;

    /// <summary>
    /// Was machine activated by user?
    /// </summary>
    [DataField]
    public bool Enabled;
}
