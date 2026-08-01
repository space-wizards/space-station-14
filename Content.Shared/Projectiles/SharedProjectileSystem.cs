using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem : EntitySystem
{
    public const string ProjectileFixture = "projectile";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileHitEvent>(OnEmbedProjectileHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate);
        SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ComponentShutdown>(OnEmbeddableCompShutdown);

        SubscribeLocalEvent<EmbeddedContainerComponent, EntityTerminatingEvent>(OnEmbeddableTermination);
    }

    private void OnEmbedActivate(Entity<EmbeddableProjectileComponent> embeddable, ref ActivateInWorldEvent args)
    {
        // Unremovable embeddables moment
        if (embeddable.Comp.RemovalTime == null)
            return;

        if (args.Handled || !args.Complex || !TryComp<PhysicsComponent>(embeddable, out var physics) ||
            physics.BodyType != BodyType.Static)
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            embeddable.Comp.RemovalTime.Value,
            new RemoveEmbeddedProjectileEvent(),
            eventTarget: embeddable,
            target: embeddable));
    }

    private void OnEmbedRemove(Entity<EmbeddableProjectileComponent> embeddable, ref RemoveEmbeddedProjectileEvent args)
    {
        if (args.Cancelled)
            return;

        EmbedDetach(embeddable, embeddable.Comp, args.User);

        // try place it in the user's hand
        _hands.TryPickupAnyHand(args.User, embeddable);
    }

    private void OnEmbeddableCompShutdown(Entity<EmbeddableProjectileComponent> embeddable, ref ComponentShutdown arg)
    {
        EmbedDetach(embeddable, embeddable.Comp);
    }

    private void OnEmbedThrowDoHit(Entity<EmbeddableProjectileComponent> embeddable, ref ThrowDoHitEvent args)
    {
        if (!embeddable.Comp.EmbedOnThrow)
            return;

        EmbedAttach(embeddable, args.Target, null, embeddable.Comp);
    }

    private void OnEmbedProjectileHit(Entity<EmbeddableProjectileComponent> embeddable, ref ProjectileHitEvent args)
    {
        EmbedAttach(embeddable, args.Target, args.Shooter, embeddable.Comp);

        // Raise a specific event for projectiles.
        if (!TryComp<ProjectileComponent>(embeddable, out var projectile))
            return;

        var ev = new ProjectileEmbedEvent(projectile.Shooter, projectile.Weapon, args.Target);
        RaiseLocalEvent(embeddable, ref ev);
    }

    private void EmbedAttach(EntityUid uid, EntityUid target, EntityUid? user, EmbeddableProjectileComponent component)
    {
        TryComp<PhysicsComponent>(uid, out var physics);
        _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        _physics.SetBodyType(uid, BodyType.Static, body: physics);
        var xform = Transform(uid);
        _transform.SetParent(uid, xform, target);

        if (component.Offset != Vector2.Zero)
        {
            var rotation = xform.LocalRotation;
            if (TryComp<ThrowingAngleComponent>(uid, out var throwingAngleComp))
                rotation += throwingAngleComp.Angle;
            _transform.SetLocalPosition(uid, xform.LocalPosition + rotation.RotateVec(component.Offset), xform);
        }

        _audio.PlayPredicted(component.Sound, uid, null);
        component.EmbeddedIntoUid = target;
        var ev = new EmbedEvent(user, target);
        RaiseLocalEvent(uid, ref ev);
        Dirty(uid, component);

        EnsureComp<EmbeddedContainerComponent>(target, out var embeddedContainer);

        //Assert that this entity not embed
        DebugTools.AssertEqual(embeddedContainer.EmbeddedObjects.Contains(uid), false);

        embeddedContainer.EmbeddedObjects.Add(uid);
    }

    public void EmbedDetach(EntityUid uid, EmbeddableProjectileComponent? component, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.EmbeddedIntoUid == null)
            return; // the entity is not embedded, so do nothing

        var embeddedInto = component.EmbeddedIntoUid;

        if (TryComp<EmbeddedContainerComponent>(component.EmbeddedIntoUid.Value, out var embeddedContainer))
        {
            embeddedContainer.EmbeddedObjects.Remove(uid);
            Dirty(component.EmbeddedIntoUid.Value, embeddedContainer);
            if (embeddedContainer.EmbeddedObjects.Count == 0)
                RemCompDeferred<EmbeddedContainerComponent>(component.EmbeddedIntoUid.Value);
        }

        if (component.DeleteOnRemove)
        {
            PredictedQueueDel(uid);
            return;
        }

        var xform = Transform(uid);
        if (TerminatingOrDeleted(xform.GridUid) && TerminatingOrDeleted(xform.MapUid))
            return;
        TryComp<PhysicsComponent>(uid, out var physics);
        _physics.SetBodyType(uid, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(uid, xform);
        component.EmbeddedIntoUid = null;
        Dirty(uid, component);

        // Reset whether the projectile has damaged anything if it successfully was removed
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            projectile.Shooter = null;
            projectile.Weapon = null;
            projectile.ProjectileSpent = false;

            Dirty(uid, projectile);
        }

        var ev = new EmbedDetachEvent(user, embeddedInto.Value);
        RaiseLocalEvent(uid, ref ev);

        if (user != null)
        {
            // Land it just coz uhhh yeah
            var landEv = new LandEvent(user, true);
            RaiseLocalEvent(uid, ref landEv);
        }

        _physics.WakeBody(uid, body: physics);
    }

    private void OnEmbeddableTermination(Entity<EmbeddedContainerComponent> container, ref EntityTerminatingEvent args)
    {
        DetachAllEmbedded(container);
    }

    public void DetachAllEmbedded(Entity<EmbeddedContainerComponent> container)
    {
        foreach (var embedded in container.Comp.EmbeddedObjects)
        {
            if (!TryComp<EmbeddableProjectileComponent>(embedded, out var embeddedComp))
                continue;

            EmbedDetach(embedded, embeddedComp);
        }
    }

    private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
    {
        if (component.IgnoreShooter && (args.OtherEntity == component.Shooter || args.OtherEntity == component.Weapon))
        {
            if (TryComp(uid, out TargetedProjectileComponent? targeted) && targeted.Target == args.OtherEntity)
                return;

            args.Cancelled = true;
        }
    }

    public void SetShooter(EntityUid id, ProjectileComponent component, EntityUid shooterId)
    {
        if (component.Shooter == shooterId)
            return;

        component.Shooter = shooterId;
        Dirty(id, component);
    }

    /// <summary>
    /// Converts fixture shapes unsupported by the physics shape-caster into an equivalent polygon.
    /// </summary>
    protected static IPhysShape GetProjectileCastShape(IPhysShape shape)
    {
        if (shape is PhysShapeCircle or PolygonShape)
            return shape;

        var bounds = shape.ComputeAABB(Robust.Shared.Physics.Transform.Empty, 0);
        for (var i = 1; i < shape.ChildCount; i++)
            bounds = bounds.Union(shape.ComputeAABB(Robust.Shared.Physics.Transform.Empty, i));

        var polygon = new PolygonShape();
        polygon.SetAsBox(bounds);
        return polygon;
    }

    /// <summary>
    /// Continuously casts the supported projectile and target shapes without Robust's unstable GJK path.
    /// </summary>
    public static bool TryCastProjectileAgainstShape(
        IPhysShape projectileShape,
        Angle projectileAngle,
        Vector2 origin,
        Vector2 translation,
        IPhysShape targetShape,
        Robust.Shared.Physics.Transform targetTransform,
        out float fraction,
        out Vector2 contactPoint)
    {
        switch (targetShape)
        {
            case PhysShapeAabb targetAabb:
                return TryCastProjectileAgainstAabb(
                    projectileShape,
                    projectileAngle,
                    origin,
                    translation,
                    targetAabb,
                    targetTransform,
                    out fraction,
                    out contactPoint);
            case PolygonShape targetPolygon when projectileShape is PolygonShape projectilePolygon:
                return TryCastProjectileAgainstPolygon(
                    projectilePolygon,
                    projectileAngle,
                    origin,
                    translation,
                    targetPolygon,
                    targetTransform,
                    out fraction,
                    out contactPoint);
            default:
                fraction = 0f;
                contactPoint = default;
                return false;
        }
    }

    /// <summary>
    /// Continuously casts a projectile against a raw AABB fixture. YAML fixtures normally become polygons
    /// during deserialization, but runtime-created AABBs still need a non-GJK path.
    /// </summary>
    private static bool TryCastProjectileAgainstAabb(
        IPhysShape projectileShape,
        Angle projectileAngle,
        Vector2 origin,
        Vector2 translation,
        PhysShapeAabb targetShape,
        Robust.Shared.Physics.Transform targetTransform,
        out float fraction,
        out Vector2 contactPoint)
    {
        fraction = 0f;
        contactPoint = default;
        if (translation.LengthSquared() < 0.000001f)
            return false;

        var targetBounds = targetShape.LocalBounds.Enlarged(targetShape.Radius);
        var targetAngle = new Angle(targetTransform.Quaternion2D.Angle);
        var relativeProjectileTransform = new Robust.Shared.Physics.Transform(
            Vector2.Zero,
            projectileAngle - targetAngle);
        var projectileBounds = projectileShape.ComputeAABB(relativeProjectileTransform, 0);
        for (var i = 1; i < projectileShape.ChildCount; i++)
            projectileBounds = projectileBounds.Union(projectileShape.ComputeAABB(relativeProjectileTransform, i));

        // Minkowski difference: positions of the projectile origin for which both shapes overlap.
        var expandedBounds = new Box2(
            targetBounds.Left - projectileBounds.Right,
            targetBounds.Bottom - projectileBounds.Top,
            targetBounds.Right - projectileBounds.Left,
            targetBounds.Top - projectileBounds.Bottom);
        var localOrigin = Robust.Shared.Physics.Transform.InvTransformPoint(targetTransform, origin);
        var localTranslation = Quaternion2D.InvRotateVector(targetTransform.Quaternion2D, translation);

        var lower = 0f;
        var upper = 1f;
        if (!ClipAxis(localOrigin.X, localTranslation.X, expandedBounds.Left, expandedBounds.Right, ref lower, ref upper) ||
            !ClipAxis(localOrigin.Y, localTranslation.Y, expandedBounds.Bottom, expandedBounds.Top, ref lower, ref upper))
        {
            return false;
        }

        fraction = lower;
        var localProjectilePosition = localOrigin + localTranslation * fraction;
        var localContact = Vector2.Clamp(
            localProjectilePosition,
            targetBounds.BottomLeft,
            targetBounds.TopRight);
        contactPoint = Robust.Shared.Physics.Transform.Mul(targetTransform, localContact);
        return true;

        static bool ClipAxis(
            float start,
            float movement,
            float minimum,
            float maximum,
            ref float lower,
            ref float upper)
        {
            if (MathF.Abs(movement) < 0.000001f)
                return start >= minimum && start <= maximum;

            var entry = (minimum - start) / movement;
            var exit = (maximum - start) / movement;
            if (entry > exit)
                (entry, exit) = (exit, entry);

            lower = MathF.Max(lower, entry);
            upper = MathF.Min(upper, exit);
            return lower <= upper && upper >= 0f && lower <= 1f;
        }
    }

    /// <summary>
    /// Uses swept separating-axis intervals for two convex polygons with fixed rotations.
    /// This avoids the GJK cast path used by Robust for polygons with a non-zero skin radius,
    /// which is explicitly known to lose ray-vs-box intersections.
    /// </summary>
    private static bool TryCastProjectileAgainstPolygon(
        PolygonShape projectileShape,
        Angle projectileAngle,
        Vector2 origin,
        Vector2 translation,
        PolygonShape targetShape,
        Robust.Shared.Physics.Transform targetTransform,
        out float fraction,
        out Vector2 contactPoint)
    {
        fraction = 0f;
        contactPoint = default;
        if (translation.LengthSquared() < 0.000001f ||
            projectileShape.VertexCount < 3 ||
            targetShape.VertexCount < 3)
        {
            return false;
        }

        var targetAngle = new Angle(targetTransform.Quaternion2D.Angle);
        var lower = 0f;
        var upper = 1f;

        foreach (var localAxis in targetShape.Normals)
        {
            var axis = targetAngle.RotateVec(localAxis);
            if (!ClipPolygonAxis(axis, ref lower, ref upper))
                return false;
        }

        foreach (var localAxis in projectileShape.Normals)
        {
            var axis = projectileAngle.RotateVec(localAxis);
            if (!ClipPolygonAxis(axis, ref lower, ref upper))
                return false;
        }

        fraction = lower;
        var projectilePosition = origin + translation * fraction;
        var localProjectilePosition = Robust.Shared.Physics.Transform.InvTransformPoint(
            targetTransform,
            projectilePosition);
        var closestPoint = targetShape.Vertices[0];
        var closestDistance = float.MaxValue;
        var inside = true;

        for (var i = 0; i < targetShape.VertexCount; i++)
        {
            var vertexA = targetShape.Vertices[i];
            var vertexB = targetShape.Vertices[(i + 1) % targetShape.VertexCount];
            if (Vector2.Dot(targetShape.Normals[i], localProjectilePosition - vertexA) > targetShape.Radius)
                inside = false;

            var edge = vertexB - vertexA;
            var edgeLengthSquared = edge.LengthSquared();
            var edgeFraction = edgeLengthSquared < 0.000001f
                ? 0f
                : Math.Clamp(Vector2.Dot(localProjectilePosition - vertexA, edge) / edgeLengthSquared, 0f, 1f);
            var point = vertexA + edge * edgeFraction;
            var distance = Vector2.DistanceSquared(localProjectilePosition, point);
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestPoint = point;
        }

        contactPoint = Robust.Shared.Physics.Transform.Mul(
            targetTransform,
            inside ? localProjectilePosition : closestPoint);
        return true;

        bool ClipPolygonAxis(Vector2 axis, ref float entryTime, ref float exitTime)
        {
            if (axis.LengthSquared() < 0.000001f)
                return true;

            axis = Vector2.Normalize(axis);
            ProjectPolygon(
                targetShape,
                targetAngle,
                targetTransform.Position,
                axis,
                out var targetMinimum,
                out var targetMaximum);
            ProjectPolygon(
                projectileShape,
                projectileAngle,
                origin,
                axis,
                out var projectileMinimum,
                out var projectileMaximum);

            var speed = Vector2.Dot(translation, axis);
            if (MathF.Abs(speed) < 0.000001f)
                return projectileMaximum >= targetMinimum && projectileMinimum <= targetMaximum;

            var axisEntry = (targetMinimum - projectileMaximum) / speed;
            var axisExit = (targetMaximum - projectileMinimum) / speed;
            if (axisEntry > axisExit)
                (axisEntry, axisExit) = (axisExit, axisEntry);

            entryTime = MathF.Max(entryTime, axisEntry);
            exitTime = MathF.Min(exitTime, axisExit);
            return entryTime <= exitTime && exitTime >= 0f && entryTime <= 1f;
        }

        static void ProjectPolygon(
            PolygonShape shape,
            Angle angle,
            Vector2 position,
            Vector2 axis,
            out float minimum,
            out float maximum)
        {
            minimum = Vector2.Dot(position + angle.RotateVec(shape.Vertices[0]), axis);
            maximum = minimum;
            for (var i = 1; i < shape.VertexCount; i++)
            {
                var projection = Vector2.Dot(position + angle.RotateVec(shape.Vertices[i]), axis);
                minimum = MathF.Min(minimum, projection);
                maximum = MathF.Max(maximum, projection);
            }

            minimum -= shape.Radius;
            maximum += shape.Radius;
        }
    }

    [Serializable, NetSerializable]
    private sealed partial class RemoveEmbeddedProjectileEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }
}

[Serializable, NetSerializable]
public sealed class ImpactEffectEvent : EntityEventArgs
{
    public string Prototype;
    public NetCoordinates Coordinates;

    public ImpactEffectEvent(string prototype, NetCoordinates coordinates)
    {
        Prototype = prototype;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Raised when an entity is just about to be hit with a projectile but can reflect it
/// </summary>
[ByRefEvent]
public record struct ProjectileReflectAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}

/// <summary>
/// Raised when a projectile hits an entity
/// </summary>
[ByRefEvent]
public record struct ProjectileHitEvent(DamageSpecifier Damage, EntityUid Target, EntityUid? Shooter = null);
