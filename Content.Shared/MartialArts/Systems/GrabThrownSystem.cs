using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.MartialArts.Components;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using System.Numerics;

namespace Content.Shared.MartialArts.Systems;

public sealed class GrabThrownSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrabThrownComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<GrabThrownComponent, StopThrowEvent>(OnStopThrow);
    }

    private void HandleCollide(EntityUid uid, GrabThrownComponent component, ref StartCollideEvent args)
    {
        if (_netMan.IsClient)   // To avoid effect spam
            return;

        if (!HasComp<ThrownItemComponent>(uid))
        {
            RemComp<GrabThrownComponent>(uid);
            return;
        }

        if (!args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        if (!HasComp<DamageableComponent>(uid))
            RemComp<GrabThrownComponent>(uid);

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

    private void OnStopThrow(EntityUid uid, GrabThrownComponent comp, StopThrowEvent args)  // We dont need this comp to exsist after fall
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
    public void Throw(
        EntityUid uid,
        Vector2 vector,
        float? staminaDamage = null,
        DamageSpecifier? damageToUid = null,
        DamageSpecifier? damageToWall = null)
    {
        _throwing.TryThrow(uid, vector, 5f, animated: false);
        
        var comp = EnsureComp<GrabThrownComponent>(uid);
        comp.StaminaDamageOnCollide = staminaDamage;
        comp.DamageOnCollide = damageToUid;
        comp.WallDamageOnCollide = damageToWall;
    }
}
