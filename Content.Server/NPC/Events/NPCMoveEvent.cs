using System.Numerics;

namespace Content.Server.NPC.Events;

/// <summary>
/// Raised directed on an NPC when getting new direction.
/// </summary>
[ByRefEvent]
public readonly record struct NPCMoveEvent(Vector2 Direction);
