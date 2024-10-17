using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Placeable;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.Teleportation;

public sealed class SuperposedSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperposedComponent, ThrownEvent>(ClearSuperposition); // This *definitely* could be done way better.
        SubscribeLocalEvent<SuperposedComponent, EntParentChangedMessage>(ClearSuperposition);
        SubscribeLocalEvent<SuperposedComponent, DroppedEvent>(OnDropped);
    }

    // Clear PossibleLocations so the entity doesn't jump suddenly jump away.
    private void ClearSuperposition<T>(EntityUid uid, SuperposedComponent superposed, T args)
    {
        superposed.PossibleLocations = [];
    }

    private void OnDropped(EntityUid uid, SuperposedComponent superposed, DroppedEvent args)
    {
        superposed.PossibleLocations = [];
        if (TryComp(uid, out TransformComponent? xform))
        {
            // Lookup all PlaceableSurfaceComponent entities nearby and add them as possible hop locations.
            var locations = _lookup.GetEntitiesInRange<PlaceableSurfaceComponent>(
                _transform.GetMapCoordinates(xform),
                superposed.SuperposeRange, LookupFlags.Dynamic | LookupFlags.Static
            );
            superposed.PossibleLocations = new EntityUid[locations.Count];
            var i = 0;
            foreach (var location in locations)
            {
                superposed.PossibleLocations[i] = location.Owner;
                i += 1;
            }
        }
    }

    // Check if superposed entity with following Transform and Superposed components is observed by near MindContainers.
    private bool IsObserved(TransformComponent xform, SuperposedComponent superposed)
    {
        var coords = _transform.GetMapCoordinates(xform);
        foreach (var mindEntity in _lookup.GetEntitiesInRange<MindContainerComponent>(coords, superposed.MaxObserverRange))
        {
            // No ghosts, please.
            if (HasComp<GhostComponent>(mindEntity.Owner))
                continue;
            if (!TryComp(mindEntity.Owner, out TransformComponent? mindXform))
                continue;

            // Only entities that are facing they the superposed entitiy are considered as actual observers.
            var relPos = coords.Position - _transform.GetWorldPosition(mindXform);
            if (relPos.Length() <= superposed.MinObserverRange || Vector2.Dot(_transform.GetWorldRotation(mindXform).ToWorldVec(), relPos) >= 0)
                return true;
        }
        return false;
    }
    
    // On each tick, all superposed entities are checked for being observed by nearby MindContainers.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Query superposed entities.
        var query = EntityQueryEnumerator<TransformComponent, SuperposedComponent>();
        while (query.MoveNext(out var uid, out var xform, out var superposed))
        {
            if (superposed.PossibleLocations.Length == 0)
                continue;
            var observedNow = IsObserved(xform, superposed);
            if (superposed.Observed == observedNow) // hops happen only when entity starts/stops being observed.
                continue;

            if (!observedNow) // if not observed anymore, select random location and hop to it.
            {
                var targetUid = superposed.PossibleLocations[_random.Next(superposed.PossibleLocations.Length)];
                if (EntityManager.TryGetComponent(targetUid, out TransformComponent? targetXform))
                {
                    _transform.SetMapCoordinates(
                        new Entity<TransformComponent>(uid, xform),
                        _transform.GetMapCoordinates(targetXform).Offset(_random.NextVector2(superposed.MaxOffset))
                    );
                }
            }
            superposed.Observed = observedNow;
        }
    }
}
