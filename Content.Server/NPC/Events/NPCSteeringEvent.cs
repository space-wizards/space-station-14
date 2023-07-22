using System.Numerics;
using Content.Server.NPC.Components;
using Content.Shared.Movement.Components;

namespace Content.Server.NPC.Events;

/// <summary>
/// Raised directed on an NPC when steering.
/// </summary>
[ByRefEvent]
public readonly record struct NPCSteeringEvent(
    InputMoverComponent Mover,
    NPCSteeringComponent Steering,
    TransformComponent Transform,
    Vector2 WorldPosition,
    Angle OffsetRotation);
