using Content.Shared.Atmos;

namespace Content.Server.Mech.Components;

[RegisterComponent]
public sealed partial class MechCabinAirComponent : Component
{
    /// <summary>
    /// Target pressure for the mech cabin (kPa).
    /// </summary>
    [DataField]
    public float TargetPressure = Atmospherics.OneAtmosphere; // ~101.3 kPa

    /// <summary>
    /// Pressure used when metering a single breath (kPa), like a tank regulator.
    /// </summary>
    [DataField]
    public float RegulatorPressure = 16f;

    /// <summary>
    /// Internal cabin air mixture separate from any attached gas cylinder.
    /// </summary>
    [DataField]
    public GasMixture Air { get; set; } = new(50f);
}
