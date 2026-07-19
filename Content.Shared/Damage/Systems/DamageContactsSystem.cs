using Content.Shared.Damage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageContactsSystem : EntitySystem
{
    private static readonly EntityTimerId DamageTimer = new("damage");

    [Dependency] private EntityTimerSystem _timers = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    [Dependency] private EntityQuery<DamageContactsComponent> _damageQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<DamageContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<DamagedByContactComponent, ComponentStartup>(OnDamagedStartup);
        SubscribeLocalEvent<DamagedByContactComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnDamagedStartup(Entity<DamagedByContactComponent> ent, ref ComponentStartup args)
    {
        var interval = TimeSpan.FromSeconds(1);
        ent.Comp.NextSecond = _timers.SetTimer(ent, DamageTimer, interval, interval);
    }

    private void OnTimer(Entity<DamagedByContactComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != DamageTimer)
            return;

        ent.Comp.NextSecond = args.NextDeadline ?? args.ScheduledTime + TimeSpan.FromSeconds(1);
        if (ent.Comp.Damage != null)
            _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, interruptsDoAfters: false);
    }

    private void OnEntityExit(EntityUid uid, DamageContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(otherUid, out var body))
            return;

        foreach (var ent in _physics.GetContactingEntities(otherUid, body))
        {
            if (ent == uid)
                continue;

            if (_damageQuery.HasComponent(ent))
                return;
        }

        RemComp<DamagedByContactComponent>(otherUid);
    }

    private void OnEntityEnter(EntityUid uid, DamageContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (HasComp<DamagedByContactComponent>(otherUid))
            return;

        if (_whitelistSystem.IsWhitelistPass(component.IgnoreWhitelist, otherUid))
            return;

        var damagedByContact = EnsureComp<DamagedByContactComponent>(otherUid);
        damagedByContact.Damage = component.Damage;
    }
}
