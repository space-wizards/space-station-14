using Content.Shared.Construction.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// This handles <see cref="MeleeThrowOnHitComponent"/>
/// </summary>
public sealed class MeleeThrowOnHitSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MeleeThrowOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<MeleeThrowOnHitComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnMeleeHit(Entity<MeleeThrowOnHitComponent> weapon, ref MeleeHitEvent args)
    {
        // TODO: MeleeHitEvent is weird. Why is this even raised if we don't hit something?
        if (!args.IsHit)
            return;

        if (_delay.IsDelayed(weapon.Owner))
            return;

        if (args.HitEntities.Count == 0)
            return;

        var userPos = _transform.GetWorldPosition(args.User);
        foreach (var target in args.HitEntities)
        {
            var targetPos = _transform.GetMapCoordinates(target).Position;
            var direction = args.Direction ?? targetPos - userPos;
            ThrowOnHitHelper(weapon, args.User, target, direction);
        }
    }

    private void OnThrowHit(Entity<MeleeThrowOnHitComponent> weapon, ref ThrowDoHitEvent args)
    {
        if (!weapon.Comp.ActivateOnThrown)
            return;

        if (!TryComp<PhysicsComponent>(args.Thrown, out var weaponPhysics))
            return;

        ThrowOnHitHelper(weapon, args.Component.Thrower, args.Target, weaponPhysics.LinearVelocity);
    }

    private void ThrowOnHitHelper(Entity<MeleeThrowOnHitComponent> ent, EntityUid? user, EntityUid target, Vector2 direction)
    {
        var attemptEvent = new AttemptMeleeThrowOnHitEvent(target, user);
        RaiseLocalEvent(ent.Owner, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        var startEvent = new MeleeThrowOnHitStartEvent(ent.Owner, user);
        RaiseLocalEvent(target, ref startEvent);

        if (ent.Comp.StunTime != null)
            _stun.TryParalyze(target, ent.Comp.StunTime.Value, false);

        if (direction == Vector2.Zero)
            return;

        _throwing.TryThrow(target, direction.Normalized() * ent.Comp.Distance, ent.Comp.Speed, user, unanchor: ent.Comp.UnanchorOnHit);
    }
}
