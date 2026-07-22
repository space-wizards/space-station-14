using Content.Shared.Sticky;
using Content.Shared.DeadSpace.Drones.Components;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnStuckSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnStuckComponent, EntityStuckEvent>(OnStuck);
    }

    private void OnStuck(Entity<TriggerOnStuckComponent> ent, ref EntityStuckEvent args)
    {
        // DS14-start: explosives attached to remote drones are armed by their dedicated action.
        if (HasComp<DetonateAttachedExplosivesComponent>(args.Target))
            return;
        // DS14-end

        Trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
