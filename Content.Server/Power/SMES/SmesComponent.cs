using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Power.SMES;

/// <summary>
///     Handles the "user-facing" side of the actual SMES object.
///     This is operations that are specific to the SMES, like UI and visuals.
///     Logic is handled in <see cref="PowerSmesSystem"/>
///     Code interfacing with the powernet is handled in <see cref="BatteryStorageComponent"/> and <see cref="BatteryDischargerComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(SmesSystem))]
public sealed class SmesComponent : Component
{
    [ViewVariables]
    public ChargeState LastChargeState;
    public TimeSpan LastChargeStateTime;

    [ViewVariables]
    public int LastChargeLevel;
    public TimeSpan LastChargeLevelTime;

    [ViewVariables]
    public ExternalPowerState LastExternalState;
    public TimeSpan LastUiUpdate;

    public static TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);
}
