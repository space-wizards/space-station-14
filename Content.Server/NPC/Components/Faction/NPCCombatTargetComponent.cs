namespace Content.Server.NPC.Components;

/// <summary>
/// Lets an NPC track what is (attempting to) damage it.
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
