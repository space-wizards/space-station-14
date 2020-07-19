using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Content.Server.Atmos
{
    public class TileAtmosInfo
    {
        public int LastCycle { get; set; } = 0;
        public long LastQueueCycle { get; set; } = 0;
        public long LastSlowQueueCycle { get; set; } = 0;
        public float MoleDelta { get; set; } = 0;
        public Dictionary<Direction, float> TransferDirections { get; } = new Dictionary<Direction, float>()
        {
            {Direction.East, 0},
            {Direction.North, 0},
            {Direction.West, 0},
            {Direction.South, 0},
        };
        public float CurrentTransferAmount { get; set; } = 0;
        public float DistanceScore { get; set; } = 0;
        public Direction CurrentTransferDirection { get; set; } = (Direction) (-1);
        public bool FastDone { get; set; } = false;

        public void Reset()
        {
            LastCycle = 0;
            LastQueueCycle = 0;
            LastSlowQueueCycle = 0;
            MoleDelta = 0;
            CurrentTransferAmount = 0;
            CurrentTransferDirection = (Direction) (-1);
            FastDone = false;

            TransferDirections.Clear();
            TransferDirections[Direction.East] = 0;
            TransferDirections[Direction.North] = 0;
            TransferDirections[Direction.West] = 0;
            TransferDirections[Direction.South] = 0;
        }
    }
}
