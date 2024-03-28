using System.Numerics;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Pulling.Components;
using Content.Shared.Turnstile.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Turnstile.Systems;

public sealed class TurnstileSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private static readonly Vector2 _mobBoundsExpansion = new Vector2(0.2f, 0.2f);

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(SharedMoverController));

        SubscribeLocalEvent<TurnstileComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<TurnstileComponent, PreventCollideEvent>(HandlePreventCollide);
    }

    private void HandlePreventCollide(Entity<TurnstileComponent> ent, ref PreventCollideEvent args)
    {
        // If turnstile is admitting this mob, allow it not to collide.
        if (ent.Comp.CurrentAdmittedMob == args.OtherEntity)
            args.Cancelled = true;

        // If Turnstile is admitting a mob, allow entities the mob is pulling to come through with it.
        if (TryComp<SharedPullerComponent>(ent.Comp.CurrentAdmittedMob, out var puller))
            args.Cancelled |= puller.Pulling == args.OtherEntity;
    }

    private void HandleCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
    {
        // The colliding entity needs to be a mob.
        if (!HasComp<MobStateComponent>(args.OtherEntity))
            return;

        if (!TryComp<TransformComponent>(ent, out var xform))
            return;

        // Check the contact normal against our direction.
        // For simplicity, we always want a mob to pass from the "back" to the "front" of the turnstile.
        // That allows unanchored turnstiles to be dragged and rotated as needed, and will admit passage in the
        // direction that they are pulled in.
        var facingDirection = xform.LocalRotation.GetDir();
        var directionOfContact = GetDirectionOfContact(xform, args.OtherEntity);

        // If the turnstile is already full with another entity or the direction of contact is incorrect...
        if (HasComp<ActiveTurnstileMarkerComponent>(ent) || facingDirection != directionOfContact)
        {
            // Reject the entity, play sound
            _audio.PlayPredicted(ent.Comp.BumpSound, ent, args.OtherEntity);
            return;
        }

        // Otherwise, admit the entity.
        ent.Comp.CurrentAdmittedMob = args.OtherEntity;

        // Play sound of turning.
        _audio.PlayPredicted(ent.Comp.TurnSound, ent, args.OtherEntity);

        // Refresh broadphase contacts to ensure correct prediction
        RefreshPhysicsState(ent, args.OtherEntity);
        EnsureComp<ActiveTurnstileMarkerComponent>(ent);
    }

    /// <summary>
    ///     Turnstiles return to their original state once their admitted mob has passed through.
    /// </summary>
    private void ReturnToIdleIfEntityPassed(Entity<TurnstileComponent> ent)
    {
        if (!HasComp<ActiveTurnstileMarkerComponent>(ent))
            return;

        // Check and see if we're still colliding with the admitted entity.
        var stillCollidingWithAdmitted = GetIsCollidingWithAdmitted(ent);
        if (stillCollidingWithAdmitted)
            return;

        RemComp<ActiveTurnstileMarkerComponent>(ent);
        ent.Comp.CurrentAdmittedMob = null;
    }

    private bool GetIsCollidingWithAdmitted(Entity<TurnstileComponent, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return false;

        if (ent.Comp1.CurrentAdmittedMob == null)
            return false;

        if (!TryComp<TransformComponent>(ent.Comp1.CurrentAdmittedMob, out var mobxform))
            return false;

        if (!TryComp<MapGridComponent>(ent.Comp2.GridUid, out var grid))
            return false;

        var tile = _map.GetTileRef(ent.Comp2.GridUid.Value, grid, ent.Comp2.Coordinates);
        var turnstileLocalBounds = _entityLookupSystem.GetLocalBounds(tile, grid.TileSize);
        var mobLocalBounds = new Box2(mobxform.LocalPosition - _mobBoundsExpansion, mobxform.LocalPosition + _mobBoundsExpansion);

        bool isColliding = turnstileLocalBounds.Intersects(mobLocalBounds);

        // Is the entity pulling something else through too? If so, the turnstile should still act as though the current mob is walking through it.
        if (TryComp<SharedPullerComponent>(ent.Comp1.CurrentAdmittedMob, out var puller))
        {
            if (puller.Pulling != null)
            {
                if (TryComp<TransformComponent>(puller.Pulling.Value, out var pulledxform))
                {
                    var pulledLocalAABB = new Box2(pulledxform.LocalPosition - _mobBoundsExpansion, pulledxform.LocalPosition + _mobBoundsExpansion);
                    isColliding |= turnstileLocalBounds.Intersects(pulledLocalAABB);
                }
            }
        }

        return isColliding;
    }

    /// <summary>
    ///     Iterate over active turnstiles and finish the rotation-admission cycle when mobs leave them.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TurnstileComponent, ActiveTurnstileMarkerComponent>();
        while (query.MoveNext(out var uid, out var turnstile, out _))
        {
            ReturnToIdleIfEntityPassed((uid, turnstile));
        }
    }

    private Direction GetDirectionOfContact(TransformComponent xform, EntityUid other)
    {
        if (!TryComp<TransformComponent>(other, out var xformOther))
            return Direction.Invalid;
        return (xform.LocalPosition - xformOther.LocalPosition).GetDir();
    }

    private void RefreshPhysicsState(Entity<TurnstileComponent, PhysicsComponent?> ent, Entity<PhysicsComponent?> other)
    {
        if (!Resolve(ent, ref ent.Comp2) || !Resolve(other, ref other.Comp))
            return;

        _physics.DestroyContacts(ent.Comp2);
        Dirty(ent, ent.Comp2);
    }
}
