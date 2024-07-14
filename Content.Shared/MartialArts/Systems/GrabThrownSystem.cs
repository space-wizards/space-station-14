using Content.Shared.Stunnable;
using Content.Shared.Damage.Components;
using Content.Shared.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.MartialArts.Components;
using Content.Shared.Damage;
using Content.Shared.Throwing;
using Content.Shared.Damage.Systems;
using System.Numerics;
using Robust.Shared.Physics.Systems;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using System.Linq;
using Content.Shared.Movement.Systems;
using Robust.Shared.Network;

namespace Content.Shared.MartialArts.Systems;

public sealed class GrabThrownSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<GrabThrownComponent, ComponentStartup>(OnStartup);
        //SubscribeLocalEvent<GrabThrownComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<GrabThrownComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<GrabThrownComponent, StopThrowEvent>(OnStopThrow);
    }

    private void HandleCollide(EntityUid uid, GrabThrownComponent component, ref StartCollideEvent args)
    {
        if (_netMan.IsClient)
            return;

        if (!HasComp<ThrownItemComponent>(uid))
        {
            RemComp<GrabThrownComponent>(uid);
            return;
        }

        if (!args.OtherFixture.Hard)
            return;

        if (!EntityManager.HasComponent<DamageableComponent>(uid))
            return;

        var speed = args.OurBody.LinearVelocity.Length();


        if (component.StaminaDamageOnCollide != null)
            _stamina.TakeStaminaDamage(uid, component.StaminaDamageOnCollide.Value);
        var damageScale = speed;

        if (component.DamageOnCollide != null)
            _damageable.TryChangeDamage(uid, component.DamageOnCollide * damageScale);

        if (component.WallDamageOnCollide != null)
            _damageable.TryChangeDamage(args.OtherEntity, component.WallDamageOnCollide * damageScale);

        //if (_gameTiming.IsFirstTimePredicted)
        //    _audio.PlayPvs(component.SoundHit, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));

        RemComp<GrabThrownComponent>(uid);
    }

    private void OnStopThrow(EntityUid uid, GrabThrownComponent comp, StopThrowEvent args)
    {
        if (HasComp<GrabThrownComponent>(uid))
            RemComp<GrabThrownComponent>(uid);
    }

    //public void OnStartup(EntityUid uid, GrabThrownComponent component, ComponentStartup args)
    //{
    //    if (_netMan.IsClient)
    //        return;

    //    if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
    //    {
    //        var fixture = fixtures.Fixtures.First();

    //        component.SavedCollisionMask = fixture.Value.CollisionMask;
    //        component.SavedCollisionLayer = fixture.Value.CollisionLayer;
            
    //        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) CollisionGroup.FlyingMobMask, fixtures);
    //       _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int) CollisionGroup.FlyingMobLayer, fixtures);
    //    }
    //}

    //public void OnShutdown(EntityUid uid, GrabThrownComponent component, ComponentShutdown args)
    //{
    //    if (_netMan.IsClient)
    //        return;

    //    if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
    //    {
    //        var fixture = fixtures.Fixtures.First();

    //        if (component.SavedCollisionLayer != null && component.SavedCollisionMask != null)
    //        {
    //            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, component.SavedCollisionMask.Value, fixtures);
    //            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, component.SavedCollisionLayer.Value, fixtures);
    //        }
    //    }
    //}

    public void Throw(EntityUid uid, Vector2 vector, float? staminaDamage = null, DamageSpecifier? damageToUid = null, DamageSpecifier? damageToWall = null)
    {
        _throwing.TryThrow(uid, vector, 4f, animated: false);
        
        var comp = EnsureComp<GrabThrownComponent>(uid);
        comp.StaminaDamageOnCollide = staminaDamage;
        comp.DamageOnCollide = damageToUid;
        comp.WallDamageOnCollide = damageToWall;
        
    }
}
