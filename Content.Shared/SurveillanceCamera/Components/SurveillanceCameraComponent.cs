using Content.Shared.DeviceNetwork;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SurveillanceCamera.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurveillanceCameraSystem))]
public sealed partial class SurveillanceCameraComponent : Component
{
    // List of active viewers. This is for bookkeeping purposes,
    // so that when a camera shuts down, any entity viewing it
    // will immediately have their subscription revoked.
    [ViewVariables]
    public HashSet<EntityUid> ActiveViewers { get; } = new();

    // Monitors != Viewers, as viewers are entities that are tied
    // to a player session that's viewing from this camera
    //
    // Monitors are grouped sets of viewers, and may be
    // completely different monitor types (e.g., monitor console,
    // AI, etc.)
    [ViewVariables]
    public HashSet<EntityUid> ActiveMonitors { get; } = new();

    // If this camera is active or not. Deactivating a camera
    // will not allow it to obtain any new viewers.
    [DataField]
    public bool Active = true;

    // This one isn't easy to deal with. Will require a UI
    // to change/set this so mapping these in isn't
    // the most terrible thing possible.
    [DataField("id")]
    public string CameraId = "camera";

    [DataField, AutoNetworkedField]
    public bool NameSet;

    [DataField, AutoNetworkedField]
    public bool NetworkSet;

    // This has to be device network frequency prototypes.
    [DataField("setupAvailableNetworks")]
    public List<ProtoId<DeviceFrequencyPrototype>> AvailableNetworks { get; private set; } = new();
}
