using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Allows an entity to be dragged around by the mouse. The position is updated for all player while dragging.
/// </summary>
[NetworkedComponent]
[RegisterComponent]
public sealed partial class TabletopDraggableComponent : Component
{
    // The player dragging the piece
    [ViewVariables]
    public NetUserId? DraggingPlayer;
}
