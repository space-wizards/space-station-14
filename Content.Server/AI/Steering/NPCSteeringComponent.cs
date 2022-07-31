using System.Threading;
using Robust.Shared.Map;

namespace Content.Server.AI.Steering;

/// <summary>
/// Added to NPCs that are moving.
/// </summary>
[RegisterComponent]
public sealed class NPCSteeringComponent : Component
{
    [ViewVariables] public CancellationTokenSource? PathfindToken;

    [ViewVariables] public Queue<TileRef> CurrentPath = new();

    [ViewVariables(VVAccess.ReadWrite)] public EntityCoordinates Coordinates;

    /// <summary>
    /// How many times we try to change our movement vector per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("movementFrequency")] public float MovementFrequency = 10f;

    /// <summary>
    /// Last input movement vector. We may re-use this so we don't re-check our velocity obstacle every tick.
    /// </summary>
    [ViewVariables] public Vector2 LastInput;
}
