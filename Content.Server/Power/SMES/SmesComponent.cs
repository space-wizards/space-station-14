using Content.Server.Power.Components;
using Content.Shared.Power;

namespace Content.Server.Power.SMES;

/// <summary>
///     Handles the "user-facing" side of the actual SMES object.
///     This is operations that are specific to the SMES, like UI and visuals.
///     Code interfacing with the powernet is handled in <see cref="BatteryStorageComponent"/> and <see cref="BatteryDischargerComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(PowerSmesSystem))]
public sealed class SmesComponent : Component
{
    /// <summary>
    /// The number of distinct charge levels a SMES has.
    /// 0 is empty max is full.
    /// </summary>
    [DataField("numChargeLevels")]
    public int NumChargeLevels = 6;

    /// <summary>
    /// The charge level of the SMES as of the most recent update.
    /// </summary>
    [ViewVariables]
    public int ChargeLevel = 0;

    /// <summary>
    /// Whether the SMES is being charged/discharged/neither.
    /// </summary>
    [ViewVariables]
    public ChargeState ChargeState = ChargeState.Still;
}
