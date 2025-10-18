using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnHitscanHitSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnHitscanHitComponent, HitscanRaycastFiredEvent>(OnHit);
    }

    private void OnHit(Entity<TriggerOnHitscanHitComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Data.HitEntity, ent.Comp.KeyOut);
    }
}
