using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.HijackBeacon;

/// <summary>
///     Component for hijack beacons.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HijackBeaconComponent : Component
{
    /// <summary>
    ///     Current state of the beacon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HijackBeaconStatus Status = HijackBeaconStatus.AWAIT_ACTIVATE;

    /// <summary>
    ///     How long it takes to deactivate the beacon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DeactivationLength = 5;

    /// <summary>
    ///     Default amount of time in seconds before it completes the hijack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Timer = 10;

    /// <summary>
    ///     Remaining time until the hijack is completed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RemainingTime;

    /// <summary>
    ///     Default amount of time before the beacon can be re-activated, if it is disarmed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Cooldown = 5;

    /// <summary>
    ///     Remaining cooldown time before the beacon can be reactivated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CooldownTime;

    /// <summary>
    ///     The entity prototype id that should be given upon objective completion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Reward = null;
}
