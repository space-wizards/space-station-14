using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    public struct TileAtmosInfo
    {
        [ViewVariables]
        public int LastCycle;

        [ViewVariables]
        public long LastQueueCycle;

        [ViewVariables]
        public long LastSlowQueueCycle;

        [ViewVariables]
        public float MoleDelta;

        [ViewVariables]
        public float TransferDirectionEast;

        [ViewVariables]
        public float TransferDirectionWest;

        [ViewVariables]
        public float TransferDirectionNorth;

        [ViewVariables]
        public float TransferDirectionSouth;

        public float this[Direction direction]
        {
            get =>
                direction switch
                {
                    Direction.East => TransferDirectionEast,
                    Direction.West => TransferDirectionWest,
                    Direction.North => TransferDirectionNorth,
                    Direction.South => TransferDirectionSouth,
                    _ => throw new ArgumentOutOfRangeException("Direction out of range!")
                };

            set
            {
                switch (direction)
                {
                    case Direction.East:
                         TransferDirectionEast = value;
                         break;
                    case Direction.West:
                        TransferDirectionWest = value;
                        break;
                    case Direction.North:
                        TransferDirectionNorth = value;
                        break;
                    case Direction.South:
                        TransferDirectionSouth = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Direction out of range!");
                }
            }
        }

        [ViewVariables]
        public float CurrentTransferAmount;

        [ViewVariables]
        public float DistanceScore;

        public Direction CurrentTransferDirection;

        [ViewVariables]
        public bool FastDone;
    }
}
