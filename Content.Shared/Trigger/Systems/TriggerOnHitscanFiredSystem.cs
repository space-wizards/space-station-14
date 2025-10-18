using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnHitscanFiredSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnHitscanFiredComponent, HitscanRaycastFiredEvent>(OnFired);
    }

    private void OnFired(Entity<TriggerOnHitscanFiredComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Data.Shooter, ent.Comp.KeyOut);
    }
}
