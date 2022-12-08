using Content.Shared.Interaction.Events;

namespace Content.Server.NPC.Events;

/// <summary>
/// Raised directed on an NPC when steering.
/// </summary>
[ByRefEvent]
public readonly record struct NPCSteeringEvent(Vector2[] Directions, float[] InterestMap, float[] DangerMap, float AgentRadius, Angle OffsetRotation)
{
    public readonly Vector2[] Directions = Directions;
    public readonly float[] InterestMap = InterestMap;
    public readonly float[] DangerMap = DangerMap;

    public readonly float AgentRadius = AgentRadius;
    public readonly Angle OffsetRotation = OffsetRotation;
}
