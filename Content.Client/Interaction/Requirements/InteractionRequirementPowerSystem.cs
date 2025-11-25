using Content.Client.Power.EntitySystems;
using Content.Shared.Interaction.Requirements;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;

namespace Content.Client.Interaction.Requirements;

public sealed class InteractionRequirementPowerSystem : SharedInteractionRequirementPowerSystem
{
    protected override bool Condition(Entity<InteractionRequirementPowerComponent> ent, ref readonly ConditionalInteractionAttemptEvent args)
    {
        if (this.IsPowered(ent.Owner, EntityManager))
            return true;

        return false;
    }
}
