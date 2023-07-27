namespace Content.Server.NPC.Components;

/// <summary>
/// Added to the target of NPC combat operators so they can see which enemies
/// are trying to kill them.
/// </summary>
[RegisterComponent]
public sealed class NPCCombatTargetComponent : Component
{
    /// <summary>
    /// Which entities are trying to kill us right now...
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> EngagingEnemies = new();
}
