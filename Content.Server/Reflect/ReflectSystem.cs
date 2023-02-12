using Content.Server.Administration.Logs;
using Content.Server.Hands.Components;
using Content.Server.Reflect;
using Content.Server.Weapons.Ranged;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Server.Popups;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Physics.Systems;

namespace Server.Content.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public sealed class ReflectSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, ProjectileCollideAttemptEvent>(TryReflectProjectile);
        SubscribeLocalEvent<HitScanShotEvent>(TryReflectHitScan);
    }

    private void TryReflectProjectile(EntityUid uid, ProjectileComponent projComp, ProjectileCollideAttemptEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physicsComp))
            return;
        if (TryComp<HandsComponent>(args.Target, out var hands))
        {
            foreach (var (_, hand) in hands.Hands)
            {
                if (TryComp<ReflectComponent>(hand.HeldEntity, out var reflect)
                    && reflect.Enabled
                    && _random.Prob(reflect.ReflectChance))
                {
                    var vel = _physics.GetMapLinearVelocity(uid);
                    var force = physicsComp.Force;
                    _physics.ResetDynamics(physicsComp);
                    _physics.ApplyForce(uid, -force);
                    _physics.SetLinearVelocity(uid, -vel);
                    projComp.Shooter = args.Target;
                    _popup.PopupEntity(Loc.GetString("reflect-projectile"), uid, PopupType.Small);
                    _audio.PlayPvs(reflect.OnReflect, uid, AudioHelpers.WithVariation(0.05f, _random));
                    _adminLogger.Add(LogType.ShotReflected, $"{ToPrettyString(args.Target):user} reflected projectile {ToPrettyString(uid):projectile}");
                    args.Cancel();
                }
            }
        }
    }

    private void TryReflectHitScan(HitScanShotEvent args)
    {
        if (args.Handled || args.User == null)
            return;
        if (TryComp<HandsComponent>(args.Target, out var hands))
        {
            foreach (var (_, hand) in hands.Hands)
            {
                if (TryComp<ReflectComponent>(hand.HeldEntity, out var reflect)
                    && reflect.Enabled
                    && _random.Prob(reflect.ReflectChance))
                {
                    _popup.PopupEntity(Loc.GetString("reflect-hit-scan"), args.Target, PopupType.Small);
                    _audio.PlayPvs(reflect.OnReflect, args.Target, AudioHelpers.WithVariation(0.05f, _random));
                    _adminLogger.Add(LogType.ShotReflected, $"{ToPrettyString(args.Target):entity} reflected hitscan shot");
                    args.Target = args.User.Value;
                    args.Handled = true;
                    return;
                }
            }
        }
    }
}
