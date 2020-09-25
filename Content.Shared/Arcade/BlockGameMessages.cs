using System;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    public static class BlockGameMessages
    {
        [Serializable, NetSerializable]
        public class BlockGamePlayerActionMessage : BoundUserInterfaceMessage
        {
            public readonly BlockGamePlayerAction PlayerAction;
            public BlockGamePlayerActionMessage(BlockGamePlayerAction playerAction)
            {
                PlayerAction = playerAction;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameVisualUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly BlockGameVisualType GameVisualType;
            public readonly BlockGameBlock[] Blocks;
            public BlockGameVisualUpdateMessage(BlockGameBlock[] blocks, BlockGameVisualType gameVisualType)
            {
                Blocks = blocks;
                GameVisualType = gameVisualType;
            }
        }

        public enum BlockGameVisualType
        {
            GameField,
            HoldBlock,
            NextBlock
        }

        [Serializable, NetSerializable]
        public class BlockGameScoreUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly int Points;
            public BlockGameScoreUpdateMessage(int points)
            {
                Points = points;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameUserStatusMessage : BoundUserInterfaceMessage
        {
            public readonly bool IsPlayer;

            public BlockGameUserStatusMessage(bool isPlayer)
            {
                IsPlayer = isPlayer;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameUserUnregisterMessage : BoundUserInterfaceMessage{}

        [Serializable, NetSerializable]
        public class BlockGameSetScreenMessage : BoundUserInterfaceMessage
        {
            public readonly BlockGameScreen Screen;
            public readonly bool isStarted;
            public BlockGameSetScreenMessage(BlockGameScreen screen, bool isStarted = true)
            {
                Screen = screen;
                this.isStarted = isStarted;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameGameOverScreenMessage : BlockGameSetScreenMessage
        {
            public readonly int FinalScore;
            public BlockGameGameOverScreenMessage(int finalScore) : base(BlockGameScreen.Gameover)
            {
                FinalScore = finalScore;
            }
        }

        [Serializable, NetSerializable]
        public enum BlockGameScreen
        {
            Game,
            Pause,
            Gameover
        }
    }
}
