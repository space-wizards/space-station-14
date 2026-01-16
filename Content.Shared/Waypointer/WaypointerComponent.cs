using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Waypointer;

/// <summary>
/// This signifies an entity with an active waypointer trying to track something.
/// This is NOT a pinpointer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WaypointerComponent : Component
{
    /// <summary>
    /// The prototype of the waypointer visible for the owner of this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<WaypointerPrototype> WaypointerProtoId;
}
