using Content.Shared.Atmos;

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// A "circulator" for the thermo-electric generator (TEG).
/// Circulators are used by the TEG to take in a side of either hot or cold gas.
/// </summary>
/// <seealso cref="TegSystem"/>
[RegisterComponent]
[Access(typeof(TegSystem))]
public sealed class TegCirculatorComponent : Component
{
    /// <summary>
    /// The difference between the inlet and outlet pressure at the start of the previous tick.
    /// </summary>
    [DataField("last_pressure_delta")] [ViewVariables(VVAccess.ReadWrite)]
    public float LastPressureDelta;

    /// <summary>
    /// The amount of moles transferred by the circulator last tick.
    /// </summary>
    [DataField("last_moles_transferred")] [ViewVariables(VVAccess.ReadWrite)]
    public float LastMolesTransferred;

    /// <summary>
    /// Minimum pressure delta between inlet and outlet for which the circulator animation speed is "fast".
    /// </summary>
    [DataField("visual_speed_delta")] [ViewVariables(VVAccess.ReadWrite)]
    public float VisualSpeedDelta = 5 * Atmospherics.OneAtmosphere;

    /// <summary>
    /// Light color of this circulator when it's running at "slow" speed.
    /// </summary>
    [DataField("light_color_slow")] [ViewVariables(VVAccess.ReadWrite)]
    public Color LightColorSlow;

    /// <summary>
    /// Light color of this circulator when it's running at "fast" speed.
    /// </summary>
    [DataField("light_color_fast")] [ViewVariables(VVAccess.ReadWrite)]
    public Color LightColorFast;
}
