using Content.Shared.Audio;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Physics.Systems;
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public abstract class SharedReflectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedHandsComponent, ProjectileReflectAttemptEvent>(TryReflectProjectile);
        SubscribeLocalEvent<SharedHandsComponent, HitScanReflectAttemptEvent>(TryReflectHitScan);
        SubscribeLocalEvent<ReflectComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ReflectComponent, ComponentGetState>(OnGetState);
    }

    private static void OnHandleState(EntityUid uid, ReflectComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ReflectComponentState state) return;
        component.Enabled = state.Enabled;
        component.Chance = state.Chance;
        component.Spread = state.Spread;
    }

    private static void OnGetState(EntityUid uid, ReflectComponent component, ref ComponentGetState args)
    {
        args.State = new ReflectComponentState(component.Enabled, component.Chance, component.Spread);
    }

    private void TryReflectProjectile(EntityUid uid, SharedHandsComponent hands, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (TryComp<ReflectComponent>(hands.ActiveHandEntity, out var reflect) &&
            reflect.Enabled && 
            _random.Prob(reflect.Chance))
        {
            var rotation = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2).Opposite();

            var relVel = _physics.GetMapLinearVelocity(args.ProjUid) - _physics.GetMapLinearVelocity(uid);
            var newVel = rotation.RotateVec(relVel);
            _physics.SetLinearVelocity(args.ProjUid, newVel);

            var locRot = Transform(args.ProjUid).LocalRotation;
            var newRot = rotation.RotateVec(locRot.ToVec());
            _transform.SetLocalRotation(args.ProjUid, newRot.ToAngle());

            _popup.PopupEntity(Loc.GetString("reflect-shot"), uid, PopupType.Small);
            _audio.PlayPvs(reflect.OnReflect, uid, AudioHelpers.WithVariation(0.05f, _random));
            args.Cancelled = true;
            return;
        }
    }

    private void TryReflectHitScan(EntityUid uid, SharedHandsComponent hands, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;
        if (TryComp<ReflectComponent>(hands.ActiveHandEntity, out var reflect) &&
            reflect.Enabled &&
            _random.Prob(reflect.Chance))
        {
            _popup.PopupEntity(Loc.GetString("reflect-shot"), uid, PopupType.Small);
            _audio.PlayPvs(reflect.OnReflect, uid, AudioHelpers.WithVariation(0.05f, _random));
            args.Reflected = true;
            var spread = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2);
            args.Direction = -spread.RotateVec(args.Direction);
            return;
        }
    }
}
