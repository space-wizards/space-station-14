using Content.Shared.Arcade;
using System.Linq;

namespace Content.Server.Arcade.BlockGame;

public sealed partial class BlockGame
{
    /// <summary>
    /// The set of types of game pieces that exist.
    /// Used as templates when creating pieces for the game.
    /// </summary>
    private readonly BlockGamePieceType[] _allBlockGamePieces;

    /// <summary>
    /// The set of types of game pieces that exist.
    /// Used to generate the templates used when creating pieces for the game.
    /// </summary>
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

    /// <summary>
    /// The set of possible rotations for the game pieces.
    /// </summary>
    private enum BlockGamePieceRotation
    {
        North,
        East,
        South,
        West
    }

    /// <summary>
    /// A static extension for the rotations that allows rotating through the possible rotations.
    /// </summary>
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

    /// <summary>
    /// A static extension for the rotations that allows rotating through the possible rotations.
    /// </summary>
    private struct BlockGamePiece
    {
        /// <summary>
        /// Where all of the blocks that make up this piece are located relative to the origin of the piece.
        /// </summary>
        public Vector2i[] Offsets;

        /// <summary>
        /// The color of all of the blocks that make up this piece.
        /// </summary>
        private BlockGameBlock.BlockGameBlockColor _gameBlockColor;

        /// <summary>
        /// Whether or not the block should be able to rotate about its origin.
        /// </summary>
        public bool CanSpin;

        /// <summary>
        /// Generates a list of the positions of each block comprising this game piece in worldspace.
        /// </summary>
        /// <param name="center">The position of the game piece in worldspace.</param>
        /// <param name="rotation">The rotation of the game piece in worldspace.</param>
        public readonly Vector2i[] Positions(Vector2i center, BlockGamePieceRotation rotation)
        {
            return RotatedOffsets(rotation).Select(v => center + v).ToArray();
        }

        /// <summary>
        /// Gets the relative position of each block comprising this piece given a rotation.
        /// </summary>
        /// <param name="rotation">The rotation to be applied to the local position of the blocks in this piece.</param>
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

        /// <summary>
        /// Gets a list of all of the blocks comprising this piece in worldspace.
        /// </summary>
        /// <param name="center">The position of the game piece in worldspace.</param>
        /// <param name="rotation">The rotation of the game piece in worldspace.</param>
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

        /// <summary>
        /// Gets a list of all of the blocks comprising this piece in worldspace.
        /// Used to generate the held piece/next piece preview images.
        /// </summary>
        public readonly BlockGameBlock[] BlocksForPreview()
        {
            var xOffset = 0;
            var yOffset = 0;
            foreach (var offset in Offsets)
            {
                if (offset.X < xOffset)
                    xOffset = offset.X;
                if (offset.Y < yOffset)
                    yOffset = offset.Y;
            }

            return Blocks(new Vector2i(-xOffset, -yOffset), BlockGamePieceRotation.North);
        }

        /// <summary>
        /// Generates a game piece for a given type of game piece.
        /// See <see cref="BlockGamePieceType"/> for the available options.
        /// </summary>
        /// <param name="type">The type of game piece to generate.</param>
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
