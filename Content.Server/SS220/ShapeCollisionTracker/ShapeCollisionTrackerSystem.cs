using System.Collections.Immutable;
using Content.Shared.Physics;
using Content.Shared.SS220.ShapeCollisionTracker;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server.SS220.ShapeCollisionTracker;

public sealed class ShapeCollisionTrackerSystem : EntitySystem
{
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    private readonly HashSet<(EntityUid, ShapeCollisionTrackerComponent)> _trackerDebounce = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ShapeCollisionTrackerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ShapeCollisionTrackerComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ShapeCollisionTrackerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShapeCollisionTrackerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShapeCollisionTrackerComponent, AnchorStateChangedEvent>(OnAnchor);
    }

    private void OnAnchor(
        EntityUid uid,
        ShapeCollisionTrackerComponent component,
        ref AnchorStateChangedEvent args)
    {
        component.Enabled = !component.RequiresAnchored ||
                            args.Anchored;

        if (!component.Enabled)
        {
            _trackerDebounce.Remove((uid, component));
            component.Colliding.Clear();
        }
        // Re-check for contacts as we cleared them.
        else if (TryComp<PhysicsComponent>(uid, out var body))
        {
            _broadphase.RegenerateContacts(uid, body);
        }
    }

    private void OnStartCollide(
        EntityUid uid,
        ShapeCollisionTrackerComponent component,
        ref StartCollideEvent args)
    {
        if (args.OurFixture.ID != ShapeCollisionTrackerComponent.FixtureID)
            return;

        component.Colliding.Add(args.OtherEntity);
        _trackerDebounce.Add((uid, component));
    }

    private void OnEndCollide(
        EntityUid uid,
        ShapeCollisionTrackerComponent component,
        ref EndCollideEvent args)
    {
        if (args.OurFixture.ID != ShapeCollisionTrackerComponent.FixtureID)
            return;

        component.Colliding.Remove(args.OtherEntity);
        _trackerDebounce.Add((uid, component));
    }

    private void OnMapInit(
        EntityUid uid,
        ShapeCollisionTrackerComponent component,
        MapInitEvent args)
    {
        component.Enabled = !component.RequiresAnchored ||
                            EntityManager.GetComponent<TransformComponent>(uid).Anchored;

        if (!TryComp<PhysicsComponent>(uid, out _))
            return;

        _fixtures.TryCreateFixture(
            uid,
            component.Shape,
            ShapeCollisionTrackerComponent.FixtureID,
            hard: false,
            collisionLayer: (int) (CollisionGroup.MidImpassable | CollisionGroup.LowImpassable | CollisionGroup.HighImpassable));
    }

    private void OnShutdown(
        EntityUid uid,
        ShapeCollisionTrackerComponent component,
        ComponentShutdown args)
    {
        component.Colliding.Clear();
        _trackerDebounce.Remove((uid, component));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var trackerPair in _trackerDebounce)
        {
            RaiseLocalEvent(trackerPair.Item1, new ShapeCollisionTrackerUpdatedEvent(trackerPair.Item2.Colliding.ToImmutableHashSet()));
        }

        _trackerDebounce.Clear();
    }

    public override void Shutdown()
    {
        _trackerDebounce.Clear();
        var trackerQuery = EntityQuery<ShapeCollisionTrackerComponent>(true);
        foreach (var trackerEntity in trackerQuery)
        {
            trackerEntity.Colliding.Clear();
        }

        base.Shutdown();
    }
}
