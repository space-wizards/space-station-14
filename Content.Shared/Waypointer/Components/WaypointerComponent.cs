using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Waypointer.Components;

/// <summary>
/// This signifies an entity with an active waypointer trying to track something.
/// This is NOT a pinpointer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WaypointerComponent : Component
{
    /// <summary>
    /// The actual UID for the action entity. It'll be saved here when the component is initialized.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The prototype ID for the action.
    /// </summary>
    [DataField]
    public EntProtoId ActionProtoId = "ActionToggleWaypointers";

    /// <summary>
    /// The prototype of the waypointer visible for the owner of this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<WaypointerPrototype>>? WaypointerProtoIds;
}
