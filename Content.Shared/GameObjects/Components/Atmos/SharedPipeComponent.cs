using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [Serializable, NetSerializable]
    public enum PipeVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class PipeVisualState
    {
        public readonly PipeDirection PipeDirection;
        public readonly ConduitLayer ConduitLayer;

        public PipeVisualState(PipeDirection pipeDirection, ConduitLayer conduitLayer)
        {
            PipeDirection = pipeDirection;
            ConduitLayer = conduitLayer;
        }
    }

    public enum PipeDirection
    {
        None = 0,

        //Half of a pipe in a direction
        North = 1 << 0,
        South = 1 << 1,
        West = 1 << 2,
        East = 1 << 3,

        //Straight pipes
        Longitudinal = North | South,
        Lateral = West | East,

        //Bends
        NWBend = North | West,
        NEBend = North | East,
        SWBend = South | West,
        SEBend = South | East,

        //T-Junctions
        TNorth = North | Lateral,
        TSouth = South | Lateral,
        TWest = West | Longitudinal,
        TEast = East | Longitudinal,

        //Four way
        Fourway = North | South | East | West,

        All = -1,
    }

    public enum PipeShape
    {
        Straight,
        Bend,
        TJunction,
        Fourway
    }

    public enum ConduitLayer
    {
        One = 1,
        Two = 2,
        Three = 3,
    }

    public static class PipeDirectionHelpers
    {
        public const int PipeDirections = 4;

        public static Angle ToAngle(this PipeDirection pipeDirection)
        {
            return pipeDirection switch
            {
                PipeDirection.East  => Angle.FromDegrees(0),
                PipeDirection.North => Angle.FromDegrees(90),
                PipeDirection.West  => Angle.FromDegrees(180),
                PipeDirection.South => Angle.FromDegrees(270),
                _ => throw new ArgumentOutOfRangeException(nameof(pipeDirection), $"{pipeDirection} does not have an associated angle."),
            };
        }

        public static PipeDirection ToPipeDirection(this Direction direction)
        {
            return direction switch
            {
                Direction.North => PipeDirection.North,
                Direction.South => PipeDirection.South,
                Direction.East  => PipeDirection.East,
                Direction.West  => PipeDirection.West,
                _ => throw new ArgumentOutOfRangeException(nameof(direction)),
            };
        }

        public static Direction ToDirection(this PipeDirection pipeDirection)
        {
            return pipeDirection switch
            {
                PipeDirection.North => Direction.North,
                PipeDirection.South => Direction.South,
                PipeDirection.East  => Direction.East,
                PipeDirection.West  => Direction.West,
                _ => throw new ArgumentOutOfRangeException(nameof(pipeDirection)),
            };
        }

        public static PipeDirection GetOpposite(this PipeDirection pipeDirection)
        {
            return pipeDirection switch
            {
                PipeDirection.North => PipeDirection.South,
                PipeDirection.South => PipeDirection.North,
                PipeDirection.East  => PipeDirection.West,
                PipeDirection.West  => PipeDirection.East,
                _ => throw new ArgumentOutOfRangeException(nameof(pipeDirection)),
            };
        }

        public static PipeShape PipeDirectionToPipeShape(this PipeDirection pipeDirection)
        {
            return pipeDirection switch
            {
                PipeDirection.Lateral       => PipeShape.Straight,
                PipeDirection.Longitudinal  => PipeShape.Straight,

                PipeDirection.NEBend        => PipeShape.Bend,
                PipeDirection.NWBend        => PipeShape.Bend,
                PipeDirection.SEBend        => PipeShape.Bend,
                PipeDirection.SWBend        => PipeShape.Bend,

                PipeDirection.TNorth        => PipeShape.TJunction,
                PipeDirection.TSouth        => PipeShape.TJunction,
                PipeDirection.TEast         => PipeShape.TJunction,
                PipeDirection.TWest         => PipeShape.TJunction,

                PipeDirection.Fourway       => PipeShape.Fourway,

                _ => throw new ArgumentOutOfRangeException(nameof(pipeDirection)),
            };
        }
    }
}
