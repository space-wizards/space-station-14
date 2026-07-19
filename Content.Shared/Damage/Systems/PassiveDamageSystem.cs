using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class PassiveDamageSystem : EntitySystem
{
    private static readonly EntityTimerId DamageTimer = new("damage");

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassiveDamageComponent, MapInitEvent>(OnPendingMapInit);
        SubscribeLocalEvent<PassiveDamageComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnPendingMapInit(EntityUid uid, PassiveDamageComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
        _timers.SetTimerAt<PassiveDamageComponent>((uid, component), DamageTimer, component.NextDamage);
    }

    private void OnTimer(Entity<PassiveDamageComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != DamageTimer)
            return;

        ent.Comp.NextDamage = args.FiredAt + TimeSpan.FromSeconds(1f);
        _timers.SetTimerAt(ent, DamageTimer, ent.Comp.NextDamage);

        if (!TryComp<DamageableComponent>(ent, out var damage) ||
            !TryComp<MobStateComponent>(ent, out var mobState))
            return;

        foreach (var allowedState in ent.Comp.AllowedStates)
        {
            if (allowedState == mobState.CurrentState)
                _damageable.ChangeDamage((ent, damage), ent.Comp.Damage, true, false);
        }
    }
}
