using System.Threading;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.CPUJob.JobQueues;
using Robust.Shared.Map;

namespace Content.Server.AI.Steering;

/// <summary>
/// Added to NPCs that are moving.
/// </summary>
[RegisterComponent]
public sealed class NPCSteeringComponent : Component
{
    [ViewVariables] public Job<Queue<TileRef>>? Pathfind = null;
    [ViewVariables] public CancellationTokenSource? PathfindToken = null;

    [ViewVariables] public Queue<TileRef> CurrentPath = new();

    /// <summary>
    /// Target that we're trying to move to.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public EntityCoordinates Coordinates;

    /// <summary>
    /// How close are we trying to get to the coordinates before being considered in range.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public float Range = 0.2f;

    /// <summary>
    /// How far does the last node in the path need to be before considering re-pathfinding.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public float RepathRange = 1f;

    [ViewVariables] public SteeringStatus Status = SteeringStatus.Moving;

    /// <summary>
    /// How many times we try to change our movement vector per second.
    /// </summary>
    // [ViewVariables(VVAccess.ReadWrite), DataField("movementFrequency")] public float MovementFrequency = 10f;

    /// <summary>
    /// Last input movement vector. We may re-use this so we don't re-check our velocity obstacle every tick.
    /// </summary>
    // [ViewVariables] public Vector2 LastInput;

    /// <summary>
    /// Accumulate time since our last input vector before we can change direction.
    /// </summary>
    // [ViewVariables(VVAccess.ReadWrite)] public float LastInputAccumulator = 0f;
}

public enum SteeringStatus : byte
{
    /// <summary>
    /// If we can't reach the target (e.g. different map).
    /// </summary>
    NoPath,

    /// <summary>
    /// Are we moving towards our target
    /// </summary>
    Moving,

    /// <summary>
    /// Are we currently in range of our target.
    /// </summary>
    InRange,
}
