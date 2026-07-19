using Content.Shared.Damage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageOnHoldingSystem : EntitySystem
{
    private static readonly EntityTimerId DamageTimer = new("damage");

    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHoldingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DamageOnHoldingComponent, EntityTimerEvent>(OnTimer);
    }

    public void SetEnabled(EntityUid uid, bool enabled, DamageOnHoldingComponent? component = null)
    {
        if (Resolve(uid, ref component))
        {
            component.Enabled = enabled;
            component.NextDamage = _timing.CurTime;
            Schedule((uid, component));
        }
    }

    private void OnMapInit(EntityUid uid, DamageOnHoldingComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime;
        Schedule((uid, component));
    }

    private void OnTimer(Entity<DamageOnHoldingComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != DamageTimer || !ent.Comp.Enabled)
            return;

        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            _damageableSystem.TryChangeDamage(container.Owner, ent.Comp.Damage, origin: ent);

        ent.Comp.NextDamage = args.FiredAt + TimeSpan.FromSeconds(ent.Comp.Interval);
        Schedule(ent);
    }

    private void Schedule(Entity<DamageOnHoldingComponent> ent)
    {
        if (ent.Comp.Enabled)
            _timers.SetTimerAt(ent, DamageTimer, ent.Comp.NextDamage);
        else
            _timers.CancelTimer<DamageOnHoldingComponent>(ent, DamageTimer);
    }
}
