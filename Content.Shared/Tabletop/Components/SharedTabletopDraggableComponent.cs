using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Components
{
    /**
     * <summary>
     * Allows an entity to be dragged around by the mouse. The position is updated for all player while dragging.
     * </summary>
     */
    [NetworkedComponent]
    public class SharedTabletopDraggableComponent : Component
    {
        public override string Name => "TabletopDraggable";

        [Serializable, NetSerializable]
        protected sealed class TabletopDraggableComponentState : ComponentState
        {
            public NetUserId? DraggingPlayer;

            public TabletopDraggableComponentState(NetUserId? draggingPlayer)
            {
                DraggingPlayer = draggingPlayer;
            }
        }
    }
}
