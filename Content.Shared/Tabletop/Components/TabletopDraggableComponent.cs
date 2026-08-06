using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Allows an entity to be dragged around by the mouse. The position is updated for all players while dragging.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TabletopDraggableComponent : Component
{
    /// <summary>
    /// The player dragging the piece.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public NetUserId? DraggingPlayer;

    /// <summary>
    /// The table this piece belongs to.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Table;
}
