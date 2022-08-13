namespace Content.Server.NPC.Components;

[RegisterComponent]
public sealed class NPCAvoidanceLookupComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<Vector2i, NPCAvoidanceChunk> Chunks = new();
}

public sealed class NPCAvoidanceChunk
{
    public readonly HashSet<EntityUid> Entities = new();
}
