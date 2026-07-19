using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Marker;

public abstract partial class SharedDamageMarkerSystem : EntitySystem
{
    private static readonly EntityTimerId ExpiryTimer = new("expiry");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, StartCollideEvent>(OnMarkerCollide);
        SubscribeLocalEvent<DamageMarkerComponent, AttackedEvent>(OnMarkerAttacked);
        SubscribeLocalEvent<DamageMarkerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<DamageMarkerComponent, EntityTimerEvent>(OnExpiry);
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

    private void OnHandleState(Entity<DamageMarkerComponent> ent, ref ComponentHandleState args)
    {
        _timers.SetTimerAt(ent, ExpiryTimer, ent.Comp.EndTime);
    }

    private void OnExpiry(Entity<DamageMarkerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == ExpiryTimer)
            RemCompDeferred<DamageMarkerComponent>(ent);
    }

    private void OnMarkerCollide(EntityUid uid, DamageMarkerOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            component.Amount <= 0 ||
            _whitelistSystem.IsWhitelistFail(component.Whitelist, args.OtherEntity) ||
            !TryComp<ProjectileComponent>(uid, out var projectile) ||
            projectile.Weapon == null)
        {
            return;
        }

        // Markers are exclusive, deal with it.
        var marker = EnsureComp<DamageMarkerComponent>(args.OtherEntity);
        marker.Damage = new DamageSpecifier(component.Damage);
        marker.Marker = projectile.Weapon.Value;
        marker.EndTime = _timing.CurTime + component.Duration;
        _timers.SetTimerAt<DamageMarkerComponent>((args.OtherEntity, marker), ExpiryTimer, marker.EndTime);
        component.Amount--;
        Dirty(args.OtherEntity, marker);

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
    }
}
