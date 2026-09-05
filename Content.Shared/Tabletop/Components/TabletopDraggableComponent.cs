using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using DrawDepthTag = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Allows an entity to be dragged around by the mouse. The position is updated for all players while dragging.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TabletopDraggableComponent : Component
{
    /// <summary>
    /// The player dragging the piece.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public NetUserId? DraggingPlayer;

    /// <summary>
    /// The scale of the piece when dragged.
    /// </summary>
    [DataField]
    public Vector2 DraggedScale = new(1.25f, 1.25f);

    /// <summary>
    /// The normal scale of the piece.
    /// </summary>
    [DataField]
    public Vector2 NormalScale = Vector2.One;

    /// <summary>
    /// The draw depth of the piece when dragged.
    /// </summary>
    [DataField]
    public int DraggedDrawDepth = (int)DrawDepthTag.Items + 1;

    /// <summary>
    /// The normal draw depth of the piece.
    /// </summary>
    [DataField]
    public int NormalDrawDepth = (int)DrawDepthTag.Items;
}
