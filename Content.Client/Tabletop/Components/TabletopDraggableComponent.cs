using Content.Shared.Tabletop.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Client.Tabletop.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedTabletopDraggableComponent))]
    public sealed class TabletopDraggableComponent : SharedTabletopDraggableComponent
    {
        // The player dragging the piece
        [ViewVariables]
        public NetUserId? DraggingPlayer;
    }
}
