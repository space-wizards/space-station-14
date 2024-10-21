using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem : EntitySystem
{
    public const string ProjectileFixture = "projectile";

    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileHitEvent>(OnEmbedProjectileHit, before: [typeof(SharedDestructibleSystem)]);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit, before: [typeof(SharedDestructibleSystem)]);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate);
        SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnRemoveEmbeddedProjectileEvent);
    }

    private void OnEmbedActivate(EntityUid uid, EmbeddableProjectileComponent component, ActivateInWorldEvent args)
    {
        // Nuh uh
        if (component.RemovalTime == null)
            return;

        if (args.Handled || !args.Complex || !TryComp<PhysicsComponent>(uid, out var physics) || physics.BodyType != BodyType.Static)
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.RemovalTime.Value,
            new RemoveEmbeddedProjectileEvent(), eventTarget: uid, target: uid));
    }

    private void OnRemoveEmbeddedProjectileEvent(Entity<EmbeddableProjectileComponent> entity, ref RemoveEmbeddedProjectileEvent args)
    {
        // Whacky prediction issues.
        if (args.Cancelled || _netManager.IsClient)
            return;

        TryUnEmbedFromParent(entity, args.User);
    }

    private void OnEmbedThrowDoHit(Entity<EmbeddableProjectileComponent> entity, ref ThrowDoHitEvent args)
    {
        if (!entity.Comp.EmbedOnThrow)
            return;

        Embed(entity, args.Target, null);
    }

    private void OnEmbedProjectileHit(Entity<EmbeddableProjectileComponent> entity, ref ProjectileHitEvent args)
    {
        Embed(entity, args.Target, args.Shooter);

        // Raise a specific event for projectiles.
        if (TryComp(entity.Owner, out ProjectileComponent? projectile))
        {
            var ev = new ProjectileEmbedEvent(projectile.Shooter!.Value, projectile.Weapon!.Value, args.Target);
            RaiseLocalEvent(entity.Owner, ref ev);
        }
    }

    private void Embed(Entity<EmbeddableProjectileComponent> projectile, EntityUid target, EntityUid? user)
    {
        EnsureComp<HasProjectilesEmbeddedComponent>(target, out var embeddeds);
        embeddeds.EmbeddedProjectiles.Add(projectile);

        var (uid, component) = projectile;

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
            _transform.SetLocalPosition(uid, xform.LocalPosition + rotation.RotateVec(component.Offset),
                xform);
        }

        _audio.PlayPredicted(component.Sound, uid, null);
        var ev = new EmbedEvent(user, target);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    ///  Makes the specified entity not be embedded in whatever it's embedded in. In the case that the specified entity
    ///  is not embedded in anything, this function does nothing.
    /// </summary>
    /// <param name="entity">The entity to make no longer embedded</param>
    /// <param name="remover">The entity which is removing the embedded entity. If not null, we'll try to put the
    /// embedded object in its hands. If null, there's no specific remover, eg. if the embeddee object is destroyed.</param>
    /// <returns>True if the entity was embedded and removed, otherwise false.</return>
    public bool TryUnEmbedFromParent(Entity<EmbeddableProjectileComponent> entity, EntityUid? remover)
    {
        var xform = Transform(entity);

        // Check that the projectile's parent has any embedded projectiles and that this projectile is one of them.
        if (!(TryComp<HasProjectilesEmbeddedComponent>(xform.ParentUid, out var c) && c is { } entitiesEmbeddedInParent && entitiesEmbeddedInParent.EmbeddedProjectiles.Contains(entity)))
        {
            return false;
        }

        // Remove `entity` from the parent's embedded projectiles, and clean up the parent's embedding component if it's empty.
        entitiesEmbeddedInParent.EmbeddedProjectiles.Remove(entity);
        if (entitiesEmbeddedInParent.EmbeddedProjectiles.Count == 0)
        {
            EntityManager.RemoveComponent<HasProjectilesEmbeddedComponent>(xform.ParentUid);
        }

        if (entity.Comp.DeleteOnRemove)
        {
            QueueDel(entity);
            return true;
        }

        TryComp<PhysicsComponent>(entity, out var physics);
        _physics.SetBodyType(entity, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(entity, xform);

        // Reset whether the projectile has damaged anything if it successfully was removed
        if (TryComp<ProjectileComponent>(entity, out var projectile))
        {
            projectile.Shooter = null;
            projectile.Weapon = null;
            projectile.DamagedEntity = false;
        }

        // Land it just coz uhhh yeah
        var landEv = new LandEvent(remover, true);
        RaiseLocalEvent(entity, ref landEv);
        _physics.WakeBody(entity, body: physics);

        // try place it in the user's hand
        if (remover is EntityUid user)
        {
            _hands.TryPickupAnyHand(user, entity);
        }

        return true;
    }

    private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
    {
        if (component.IgnoreShooter && (args.OtherEntity == component.Shooter || args.OtherEntity == component.Weapon))
        {
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
public record struct ProjectileReflectAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled);

/// <summary>
/// Raised when a projectile hits an entity
/// </summary>
[ByRefEvent]
public record struct ProjectileHitEvent(DamageSpecifier Damage, EntityUid Target, EntityUid? Shooter = null);
