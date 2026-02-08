using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Waypointer.Components;

/// <summary>
///  This is used for clothing that enables waypointers for the equipee.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingShowWaypointerComponent: Component
{
    /// <summary>
    /// The prototype of the waypointer that this clothing will grant to the wearer.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<WaypointerPrototype>> WaypointerProtoIds = default!;
}
