using Content.Server.Tesla.EntitySystems;

namespace Content.Server.Tesla.Components;

/// <summary>
/// Generates electricity from lightning bolts
/// </summary>
[RegisterComponent, Access(typeof(TeslaCoilSystem))]
public sealed partial class TeslaCoilComponent : Component
{
    /// <summary>
    /// How much power will the coil generate from a lightning strike
    /// </summary>
    // To Do: Different lightning bolts have different powers and generate different amounts of energy
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChargeFromLightning = 50000f;
}
