using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnHitscanSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnHitscanHitComponent, HitscanRaycastFiredEvent>(OnHit);
        SubscribeLocalEvent<TriggerOnHitscanFiredComponent, HitscanRaycastFiredEvent>(OnFired);
    }

    private void OnHit(Entity<TriggerOnHitscanHitComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        Trigger.Trigger(ent.Owner, args.Data.HitEntity, ent.Comp.KeyOut);
    }

    private void OnFired(Entity<TriggerOnHitscanFiredComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        Trigger.Trigger(ent.Owner, args.Data.Shooter, ent.Comp.KeyOut);
    }
}
