using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop
{
    public class SharedTabletopSystem : EntitySystem
    {
        [Serializable, NetSerializable]
        public sealed class TabletopDraggableComponentState : ComponentState
        {
            public NetUserId? DraggingPlayer;

            public TabletopDraggableComponentState(NetUserId? draggingPlayer)
            {
                DraggingPlayer = draggingPlayer;
            }
        }
    }
}
