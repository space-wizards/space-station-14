using Content.Server.AI.Components;

namespace Content.Server.AI.Systems;

/// <summary>
/// Handles melee combat for NPCs. The logic isn't on HTNOperator as we may need to coordinate
/// a bunch between NPCs and handle mechanics like juking.
/// </summary>
public sealed class NPCCombatSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<NPCMeleeCombatComponent>())
        {
            // TODO: DO the thing
            // Need to be able to specify: Accuracy on moving targets
            // Should we hit until crit
            // Should we hit until destroyed
            // Juking
            // If target range above threshold (e.g. 0.7f) then move back into range
        }
    }
}
