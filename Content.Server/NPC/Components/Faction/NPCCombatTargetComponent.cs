using Content.Server.NPC.Systems.Faction;

namespace Content.Server.NPC.Components.Faction;

/// <summary>
/// Lets an NPC track what is (attempting to) damage it.
/// </summary>
[RegisterComponent]
[Access(typeof(NPCCombatTargetSystem))]
public sealed class NPCCombatTargetComponent : Component
{
    /// <summary>
    /// Which entities are trying to kill us right now...
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> EngagingEnemies = new();
}
