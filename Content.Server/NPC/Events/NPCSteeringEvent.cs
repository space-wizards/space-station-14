using Content.Server.NPC.Components;

namespace Content.Server.NPC.Events;

/// <summary>
/// Raised directed on an NPC when steering.
/// </summary>
[ByRefEvent]
public readonly record struct NPCSteeringEvent(
    NPCSteeringComponent Steering,
    float[] Interest,
    float[] Danger,
    float AgentRadius,
    Angle OffsetRotation,
    Vector2 WorldPosition)
{
    public readonly NPCSteeringComponent Steering = Steering;
    public readonly float[] Interest = Interest;
    public readonly float[] Danger = Danger;

    public readonly float AgentRadius = AgentRadius;
    public readonly Angle OffsetRotation = OffsetRotation;
    public readonly Vector2 WorldPosition = WorldPosition;
}
