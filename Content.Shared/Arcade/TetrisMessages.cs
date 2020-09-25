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
            public readonly TetrisUIBlockType Type;
            public readonly TetrisBlock[] Blocks;
            public TetrisUIUpdateMessage(TetrisBlock[] blocks, TetrisUIBlockType type)
            {
                Blocks = blocks;
                Type = type;
            }
        }

        public enum TetrisUIBlockType
        {
            GameField,
            HoldBlock,
            NextBlock
        }

        [Serializable, NetSerializable]
        public class TetrisScoreUpdate : BoundUserInterfaceMessage
        {
            public readonly int Points;
            public TetrisScoreUpdate(int points)
            {
                Points = points;
            }
        }

        [Serializable, NetSerializable]
        public class TetrisUserMessage : BoundUserInterfaceMessage
        {
            public readonly bool IsPlayer;

            public TetrisUserMessage(bool isPlayer)
            {
                IsPlayer = isPlayer;
            }
        }

        [Serializable, NetSerializable]
        public class TetrisUserUnregisterMessage : BoundUserInterfaceMessage{}

        [Serializable, NetSerializable]
        public class TetrisSetScreenMessage : BoundUserInterfaceMessage
        {
            public readonly TetrisScreen Screen;
            public readonly bool isStarted;
            public TetrisSetScreenMessage(TetrisScreen screen, bool isStarted = true)
            {
                Screen = screen;
                this.isStarted = isStarted;
            }
        }

        [Serializable, NetSerializable]
        public class TetrisGameOverScreenMessage : TetrisSetScreenMessage
        {
            public readonly int FinalScore;
            public TetrisGameOverScreenMessage(int finalScore) : base(TetrisScreen.Gameover)
            {
                FinalScore = finalScore;
            }
        }

        [Serializable, NetSerializable]
        public enum TetrisScreen
        {
            Game,
            Pause,
            Gameover
        }
    }
}
