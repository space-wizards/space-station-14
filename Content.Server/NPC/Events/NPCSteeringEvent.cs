namespace Content.Server.NPC.Events;

/// <summary>
/// Raised directed on an NPC when steering.
/// </summary>
[ByRefEvent]
public readonly record struct NPCSteeringEvent(float[] InterestMap, float[] DangerMap)
{
    public readonly float[] InterestMap = InterestMap;
    public readonly float[] DangerMap = DangerMap;
}
