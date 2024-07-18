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

        if (!args.OurFixture.Hard || !args.OtherFixture.Hard)
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

        _color.RaiseEffect(Color.Red, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));

        RemComp<GrabThrownComponent>(uid);
    }

    private void OnStopThrow(EntityUid uid, GrabThrownComponent comp, StopThrowEvent args)
    {
        if (HasComp<GrabThrownComponent>(uid))
            RemComp<GrabThrownComponent>(uid);
    }

    /// <summary>
    /// Throwing entity to the direction and ensures GrabThrownComponent with params
    /// </summary>
    /// <param name="uid">Entity to throw</param>
    /// <param name="vector">Direction</param>
    /// <param name="staminaDamage">Stamina damage on collide</param>
    /// <param name="damageToUid">Damage to entity on collide</param>
    /// <param name="damageToWall">Damage to wall or anything that was hit by entity</param>
    public void Throw(EntityUid uid, Vector2 vector, float? staminaDamage = null, DamageSpecifier? damageToUid = null, DamageSpecifier? damageToWall = null)
    {
        _throwing.TryThrow(uid, vector, 5f, animated: false);
        
        var comp = EnsureComp<GrabThrownComponent>(uid);
        comp.StaminaDamageOnCollide = staminaDamage;
        comp.DamageOnCollide = damageToUid;
        comp.WallDamageOnCollide = damageToWall;
    }
}
