using Content.Server.Administration.Logs;
using Content.Server.Hands.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Random;
using Robust.Shared.Physics.Systems;
using Content.Server.Weapons.Reflect;
using Content.Shared.Weapons.Ranged;
using Content.Server.Weapons.Melee.EnergySword;

namespace Server.Content.Weapons.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public sealed class ReflectSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, PreventProjectileCollideEvent>(TryReflectProjectile);
        SubscribeLocalEvent<HitScanShotEvent>(TryReflectHitScan);

        SubscribeLocalEvent<ReflectComponent, EnergySwordActivatedEvent>(EnableReflect);
        SubscribeLocalEvent<ReflectComponent, EnergySwordDeactivatedEvent>(DisableReflect);
    }

    private void TryReflectProjectile(EntityUid uid, HandsComponent hands, ref PreventProjectileCollideEvent args)
    {
        foreach (var (_, hand) in hands.Hands)
        {
            if (TryComp<ReflectComponent>(hand.HeldEntity, out var reflect) &&
                reflect.Enabled && 
                _random.Prob(reflect.Chance))
            {
                var vel = _physics.GetMapLinearVelocity(uid) - _physics.GetMapLinearVelocity(args.ProjUid);
                var spread = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2);
                vel = spread.RotateVec(vel);
                _physics.SetLinearVelocity(args.ProjUid, vel);
                _transform.SetWorldRotation(args.ProjUid, vel.ToWorldAngle());
                _projectile.SetShooter(args.ProjComp, uid);
                _popup.PopupEntity(Loc.GetString("reflect-shot"), uid, PopupType.Small);
                _audio.PlayPvs(reflect.OnReflect, uid, AudioHelpers.WithVariation(0.05f, _random));
                _adminLogger.Add(LogType.ShotReflected, $"{ToPrettyString(uid):user} reflected projectile {ToPrettyString(args.ProjUid):projectile}");
                args.Cancelled = true;
                return;
            }
        }
    }

    private void TryReflectHitScan(ref HitScanShotEvent args)
    {
        if (args.User == null)
            return;
        if (TryComp<HandsComponent>(args.Target, out var hands))
        {
            foreach (var (_, hand) in hands.Hands)
            {
                if (TryComp<ReflectComponent>(hand.HeldEntity, out var reflect)
                    && reflect.Enabled
                    && _random.Prob(reflect.Chance))
                {
                    _popup.PopupEntity(Loc.GetString("reflect-shot"), args.Target, PopupType.Small);
                    _audio.PlayPvs(reflect.OnReflect, args.Target, AudioHelpers.WithVariation(0.05f, _random));
                    _adminLogger.Add(LogType.ShotReflected, $"{ToPrettyString(args.Target):entity} reflected hitscan shot");
                    args.Target = args.User.Value;
                    return;
                }
            }
        }
    }

    private void EnableReflect(EntityUid uid, ReflectComponent comp, ref EnergySwordActivatedEvent args)
    {
        comp.Enabled = true;
    }

    private void DisableReflect(EntityUid uid, ReflectComponent comp, ref EnergySwordDeactivatedEvent args)
    {
        comp.Enabled = false;
    }
}
