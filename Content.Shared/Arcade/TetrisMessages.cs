using System;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    public static class TetrisMessages
    {
        [Serializable, NetSerializable]
        public class TetrisPlayerActionMessage : BoundUserInterfaceMessage
        {
            public readonly TetrisPlayerAction PlayerAction;
            public TetrisPlayerActionMessage(TetrisPlayerAction playerAction)
            {
                PlayerAction = playerAction;
            }
        }

        [Serializable, NetSerializable]
        public class TetrisUIUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly TetrisBlock[] Blocks;
            public TetrisUIUpdateMessage(TetrisBlock[] blocks)
            {
                Blocks = blocks;
            }
        }
    }
}
