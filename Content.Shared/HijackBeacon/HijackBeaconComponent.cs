using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.HijackBeacon;

/// <summary>
///     Component for hijack beacons.
/// </summary>
/// <remarks>
///     Status and timer fields are private so the state machine is preserved.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HijackBeaconComponent : Component
{
    /// <summary>
    ///     Current state of the beacon.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(HijackBeaconSystem))]
    public HijackBeaconStatus Status = HijackBeaconStatus.AWAIT_ACTIVATE;

    /// <summary>
    ///     How long it takes to deactivate the beacon.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan DeactivationLength = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Remaining time until the hijack is completed.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan RemainingTime = TimeSpan.FromSeconds(200);

    /// <summary>
    ///     The minimum amount of time on the timer if the beacon is reactivated.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan MinimumTime = TimeSpan.FromSeconds(100);

    /// <summary>
    ///     Default amount of time before the beacon can be re-activated, if it is disarmed.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    /// <summary>
    ///     Remaining cooldown time before the beacon can be reactivated.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan CooldownTime = TimeSpan.Zero;

    /// <summary>
    ///     The entity prototype id that should be given upon objective completion, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Reward = null;
}
