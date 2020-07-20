using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared.Maths;

namespace Content.Server.Atmos
{
    public struct TileAtmosInfo
    {
        public int LastCycle;
        public long LastQueueCycle;
        public long LastSlowQueueCycle;
        public float MoleDelta;

        public float TransferDirectionEast;
        public float TransferDirectionWest;
        public float TransferDirectionNorth;
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

        public float CurrentTransferAmount;
        public float DistanceScore;
        public Direction CurrentTransferDirection;
        public bool FastDone;
    }
}
