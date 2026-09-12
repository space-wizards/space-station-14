using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.HijackBeacon;

/// <summary>
///     Component for hijack beacons, meant to be planted on the ATS to drain station funds.
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
    public HijackBeaconStatus Status = HijackBeaconStatus.AwaitActivate;

    /// <summary>
    ///     How long it takes to deactivate the beacon.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan DeactivationLength = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     How long it takes for the beacon to hack.
    /// </summary>
    [DataField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan HackTime = TimeSpan.FromSeconds(200);

    /// <summary>
    ///     Default amount of time before the beacon can be re-activated, if it is disarmed.
    /// </summary>
    [DataField, Access(typeof(HijackBeaconSystem))]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    /// <summary>
    ///     How much cash should be withdrawn from each department account?
    /// </summary>
    [DataField]
    public int Fine = 5000;
}
