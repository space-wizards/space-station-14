using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Marker;

public abstract class SharedDamageMarkerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, StartCollideEvent>(OnMarkerCollide);
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<DamageMarkerComponent, AttackedEvent>(OnMarkerAttacked);
    }

    private void OnMarkerAttacked(EntityUid uid, DamageMarkerComponent component, AttackedEvent args)
    {
        if (component.Marker != args.Used)
            return;

        args.BonusDamage += component.Damage;
        RemCompDeferred<DamageMarkerComponent>(uid);
        _audio.PlayPredicted(component.Sound, uid, args.User);

        if (TryComp<LeechOnMarkerComponent>(args.Used, out var leech))
        {
            _damageable.TryChangeDamage(args.User, leech.Leech, true, false, origin: args.Used);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamageMarkerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime > _timing.CurTime)
                continue;

            RemCompDeferred<DamageMarkerComponent>(uid);
        }
    }

    private void OnMarkerCollide(EntityUid uid, DamageMarkerOnCollideComponent component, ref StartCollideEvent args)
    {
        if (HasComp<ProjectileComponent>(uid) ||
            !args.OtherFixture.Hard ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            !TryApplyMarker(uid, component, args.OtherEntity))
        {
            return;
        }
    }

    private void OnProjectileHit(Entity<DamageMarkerOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        TryApplyMarker(ent, ent.Comp, args.Target);
    }

    private bool TryApplyMarker(
        EntityUid uid,
        DamageMarkerOnCollideComponent component,
        EntityUid target)
    {
        if (component.Amount <= 0 ||
            _whitelistSystem.IsWhitelistFail(component.Whitelist, target) ||
            !TryComp<ProjectileComponent>(uid, out var projectile) ||
            projectile.Weapon == null)
        {
            return false;
        }

        // Markers are exclusive, deal with it.
        var marker = EnsureComp<DamageMarkerComponent>(target);
        marker.Damage = new DamageSpecifier(component.Damage);
        marker.Marker = projectile.Weapon.Value;
        marker.EndTime = _timing.CurTime + component.Duration;
        component.Amount--;
        Dirty(target, marker);

        if (_netManager.IsServer)
        {
            if (component.Amount <= 0)
            {
                QueueDel(uid);
            }
            else
            {
                Dirty(uid, component);
            }
        }

        return true;
    }
}
