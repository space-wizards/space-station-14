namespace Content.Server.NPC.Components;

/// <summary>The current leader and remembered friendly and hostile entities of an imprinted NPC.</summary>
[RegisterComponent]
public sealed partial class NPCImprintedComponent : Component
{
    /// <summary>
    /// The entity this NPC is following and attacking point targets from.
    /// </summary>
    [DataField]
    public EntityUid? Leader;


    /// <summary>
    /// A list of entities this NPC is friendly to - if hostile it won't attack them.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Friendly = new();

    /// <summary>
    /// A list of entities this NPC is hostile to - it will attack them on sight.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Target = new();
}
