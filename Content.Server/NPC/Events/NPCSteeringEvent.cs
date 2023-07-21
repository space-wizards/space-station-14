using Content.Server.NPC.Components;

namespace Content.Server.NPC.Events;

/// <summary>
/// Raised directed on an NPC when steering.
/// </summary>
[ByRefEvent]
public readonly record struct NPCSteeringEvent(
    NPCSteeringComponent Steering)
{
    public readonly NPCSteeringComponent Steering = Steering;
}
