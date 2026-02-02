using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Waypointer.Components;

/// <summary>
///  This is used for entities that have an innate waypointer.
/// </summary>
/// <example>
/// Dragons.
/// </example>
[RegisterComponent, NetworkedComponent]
public sealed partial class InnateWaypointerComponent: Component
{
    /// <summary>
    /// The prototype of the waypointer that this entity will have.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<WaypointerPrototype>> WaypointerProtoIds = default!;
}
