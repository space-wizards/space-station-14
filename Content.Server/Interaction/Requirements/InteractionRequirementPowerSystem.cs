using Content.Shared.Interaction;
using Content.Shared.Interaction.Requirements;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Power.EntitySystems;

public sealed class InteractionRequirementPowerSystem : SharedInteractionRequirementPowerSystem
{
    protected override bool TrackInteractions => true;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractionRequirementPowerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    protected override bool Condition(Entity<InteractionRequirementPowerComponent> ent, ref readonly ConditionalInteractionAttemptEvent args)
    {
        if (this.IsPowered(ent.Owner, EntityManager))
            return true;

        return false;
    }

    private void OnPowerChanged(Entity<InteractionRequirementPowerComponent> ent, ref PowerChangedEvent args)
    {
        NotifyRequirementChange(ent, args.Powered);
    }
}
