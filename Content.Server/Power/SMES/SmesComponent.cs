using Content.Server.Power.Components;
using Content.Shared.Power;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Power.SMES;

/// <summary>
///     Handles the "user-facing" side of the actual SMES object.
///     This is operations that are specific to the SMES, like UI and visuals.
///     Logic is handled in <see cref="SmesSystem"/>
///     Code interfacing with the powernet is handled in <see cref="BatteryStorageComponent"/> and <see cref="BatteryDischargerComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(SmesSystem))]
public sealed partial class SmesComponent : Component
{
    [DataField("lastChargeState")]
    public ChargeState LastChargeState;
    [DataField("lastChargeStateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastChargeStateTime;

    [DataField("lastChargeLevel")]
    public int LastChargeLevel;
    [DataField("lastChargeLevelTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastChargeLevelTime;

    [DataField("lastExternalState")]
    public ExternalPowerState LastExternalState;
    [DataField("lastUiUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastUiUpdate;

    /// <summary>
    /// The number of distinct charge levels a SMES has.
    /// 0 is empty max is full.
    /// </summary>
    [DataField("numChargeLevels")]
    public int NumChargeLevels = 6;

    /// <summary>
    /// The charge level of the SMES as of the most recent update.
    /// </summary>
    [DataField("chargeLevel")]
    public int ChargeLevel = 0;

    /// <summary>
    /// Whether the SMES is being charged/discharged/neither.
    /// </summary>
    [DataField("chargeState")]
    public ChargeState ChargeState = ChargeState.Still;

    public static TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);
}
