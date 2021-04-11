#nullable enable
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;

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
        // TODO ATMOS: Make this not a class and just be the field below...
        public readonly PipeDirection ConnectedDirections;

        public PipeVisualState(PipeDirection connectedDirections)
        {
            ConnectedDirections = connectedDirections;
        }
    }

    [Flags]
    [Serializable, NetSerializable]
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
        Half,
        Straight,
        Bend,
        TJunction,
        Fourway
    }

    public static class PipeShapeHelpers
    {
        /// <summary>
        ///     Gets the direction of a shape when facing 0 degrees (the initial direction of entities).
        /// </summary>
        public static PipeDirection ToBaseDirection(this PipeShape shape)
        {
            return shape switch
            {
                PipeShape.Half => PipeDirection.South,
                PipeShape.Straight => PipeDirection.Longitudinal,
                PipeShape.Bend => PipeDirection.SWBend,
                PipeShape.TJunction => PipeDirection.TSouth,
                PipeShape.Fourway => PipeDirection.Fourway,
                _ => throw new ArgumentOutOfRangeException(nameof(shape), $"{shape} does not have an associated {nameof(PipeDirection)}."),
            };
        }
    }

    public static class PipeDirectionHelpers
    {
        public const int PipeDirections = 4;

        public static bool HasDirection(this PipeDirection pipeDirection, PipeDirection other)
        {
            return (pipeDirection & other) == other;
        }

        public static Angle ToAngle(this PipeDirection pipeDirection)
        {
            return pipeDirection.ToDirection().ToAngle();
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
                PipeDirection.North         => PipeShape.Half,
                PipeDirection.South         => PipeShape.Half,
                PipeDirection.East          => PipeShape.Half,
                PipeDirection.West          => PipeShape.Half,

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

        public static PipeDirection RotatePipeDirection(this PipeDirection pipeDirection, double diff)
        {
            var newPipeDir = PipeDirection.None;
            for (var i = 0; i < PipeDirections; i++)
            {
                var currentPipeDirection = (PipeDirection) (1 << i);
                if (!pipeDirection.HasFlag(currentPipeDirection)) continue;
                var angle = currentPipeDirection.ToAngle();
                angle += diff;
                newPipeDir |= angle.GetCardinalDir().ToPipeDirection();
            }
            return newPipeDir;
        }
    }
}
