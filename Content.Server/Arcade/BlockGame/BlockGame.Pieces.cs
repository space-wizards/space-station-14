using Content.Shared.Arcade;
using System.Linq;

namespace Content.Server.Arcade.BlockGame;

public sealed partial class BlockGame
{
    private readonly BlockGamePieceType[] _allBlockGamePieces;

    private enum BlockGamePieceType
    {
        I,
        L,
        LInverted,
        S,
        SInverted,
        T,
        O
    }

    private enum BlockGamePieceRotation
    {
        North,
        East,
        South,
        West
    }

    private static BlockGamePieceRotation Next(BlockGamePieceRotation rotation, bool inverted)
    {
        return rotation switch
        {
            BlockGamePieceRotation.North => inverted ? BlockGamePieceRotation.West : BlockGamePieceRotation.East,
            BlockGamePieceRotation.East => inverted ? BlockGamePieceRotation.North : BlockGamePieceRotation.South,
            BlockGamePieceRotation.South => inverted ? BlockGamePieceRotation.East : BlockGamePieceRotation.West,
            BlockGamePieceRotation.West => inverted ? BlockGamePieceRotation.South : BlockGamePieceRotation.North,
            _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
        };
    }

    private struct BlockGamePiece
    {
        public Vector2i[] Offsets;
        private BlockGameBlock.BlockGameBlockColor _gameBlockColor;
        public bool CanSpin;

        public readonly Vector2i[] Positions(Vector2i center, BlockGamePieceRotation rotation)
        {
            return RotatedOffsets(rotation).Select(v => center + v).ToArray();
        }

        private readonly Vector2i[] RotatedOffsets(BlockGamePieceRotation rotation)
        {
            var rotatedOffsets = (Vector2i[]) Offsets.Clone();
            //until i find a better algo
            var amount = rotation switch
            {
                BlockGamePieceRotation.North => 0,
                BlockGamePieceRotation.East => 1,
                BlockGamePieceRotation.South => 2,
                BlockGamePieceRotation.West => 3,
                _ => 0
            };

            for (var i = 0; i < amount; i++)
            {
                for (var j = 0; j < rotatedOffsets.Length; j++)
                {
                    rotatedOffsets[j] = rotatedOffsets[j].Rotate90DegreesAsOffset();
                }
            }

            return rotatedOffsets;
        }

        public readonly BlockGameBlock[] Blocks(Vector2i center, BlockGamePieceRotation rotation)
        {
            var positions = Positions(center, rotation);
            var result = new BlockGameBlock[positions.Length];
            var i = 0;
            foreach (var position in positions)
            {
                result[i++] = position.ToBlockGameBlock(_gameBlockColor);
            }

            return result;
        }

        public readonly BlockGameBlock[] BlocksForPreview()
        {
            var xOffset = 0;
            var yOffset = 0;
            foreach (var offset in Offsets)
            {
                if (offset.X < xOffset) xOffset = offset.X;
                if (offset.Y < yOffset) yOffset = offset.Y;
            }

            return Blocks(new Vector2i(-xOffset, -yOffset), BlockGamePieceRotation.North);
        }

        public static BlockGamePiece GetPiece(BlockGamePieceType type)
        {
            //switch statement, hardcoded offsets
            return type switch
            {
                BlockGamePieceType.I => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(0, 2),
                    },
                    _gameBlockColor = BlockGameBlock.BlockGameBlockColor.LightBlue,
                    CanSpin = true
                },
                BlockGamePieceType.L => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(1, 1),
                    },
                    _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Orange,
                    CanSpin = true
                },
                BlockGamePieceType.LInverted => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(-1, 1),
                        new Vector2i(0, 1),
                    },
                    _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Blue,
                    CanSpin = true
                },
                BlockGamePieceType.S => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(-1, 0),
                        new Vector2i(0, 0),
                    },
                    _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Green,
                    CanSpin = true
                },
                BlockGamePieceType.SInverted => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(-1, -1), new Vector2i(0, -1), new Vector2i(0, 0),
                        new Vector2i(1, 0),
                    },
                    _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Red,
                    CanSpin = true
                },
                BlockGamePieceType.T => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(0, -1),
                        new Vector2i(-1, 0), new Vector2i(0, 0), new Vector2i(1, 0),
                    },
                    _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Purple,
                    CanSpin = true
                },
                BlockGamePieceType.O => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(0, 0),
                        new Vector2i(1, 0),
                    },
                    _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Yellow,
                    CanSpin = false
                },
                _ => new BlockGamePiece
                {
                    Offsets = new[]
                    {
                        new Vector2i(0, 0)
                    }
                },
            };
        }
    }
}
