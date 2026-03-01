using Content.Shared.Damage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class DamageContactsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    private EntityQuery<DamageContactsComponent> _damageContactsQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageContactsComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<DamageContactsComponent, EndCollideEvent>(OnEndCollide);

        _damageContactsQuery = GetEntityQuery<DamageContactsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamagedByContactComponent>();

        while (query.MoveNext(out var uid, out var damaged))
        {
            if (_timing.CurTime < damaged.NextSecond)
                continue;

            damaged.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
            Dirty(uid, damaged);

            if (damaged.Damage != null)
                _damageable.TryChangeDamage(uid, damaged.Damage, interruptsDoAfters: false);
        }
    }

    private void OnEndCollide(Entity<DamageContactsComponent> ent, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(otherUid, out var body))
            return;

        foreach (var contact in _physics.GetContactingEntities(otherUid, body))
        {
            if (contact == ent.Owner)
                continue;

            if (_damageContactsQuery.HasComp(contact))
                return;
        }

        RemComp<DamagedByContactComponent>(otherUid);
    }

    private void OnStartCollide(Entity<DamageContactsComponent> ent, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (HasComp<DamagedByContactComponent>(otherUid))
            return;

        if (_whitelistSystem.IsWhitelistPass(ent.Comp.IgnoreWhitelist, otherUid))
            return;

        var damagedByContact = EnsureComp<DamagedByContactComponent>(otherUid);
        damagedByContact.Damage = ent.Comp.Damage;
        damagedByContact.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
        Dirty(otherUid, damagedByContact);
    }
}
