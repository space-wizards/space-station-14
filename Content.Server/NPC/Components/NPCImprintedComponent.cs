namespace Content.Server.NPC.Components;

/// <summary>The current leader and remembered friendly and hostile entities of an imprinted NPC.</summary>
[RegisterComponent]
public sealed partial class NPCImprintedComponent : Component
{
    [DataField]
    public EntityUid? Leader;

    [DataField]
    public HashSet<EntityUid> Friendly = new();

    [DataField]
    public HashSet<EntityUid> Target = new();
}
