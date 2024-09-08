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

    private void ClearSuperposition<T>(EntityUid uid, SuperposedComponent superposed, T args)
    {
        superposed.PossibleLocations = [];
    }

    private void OnDropped(EntityUid uid, SuperposedComponent superposed, DroppedEvent args)
    {
        superposed.PossibleLocations = [];
        if (TryComp(uid, out TransformComponent? xform))
        {
            var locations = _lookup.GetEntitiesInRange<PlaceableSurfaceComponent>(_transform.GetMapCoordinates(xform), superposed.SuperposeRange, LookupFlags.Dynamic | LookupFlags.Static);
            superposed.PossibleLocations = new EntityUid[locations.Count];
            var i = 0;
            foreach (var location in locations)
            {
                superposed.PossibleLocations[i] = location.Owner;
                i += 1;
            }
        }
    }

    // eating

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TransformComponent, SuperposedComponent>();
        while (query.MoveNext(out var uid, out var xform, out var superposed))
        {
            if (superposed.PossibleLocations.Length == 0)
                continue;
            var observedNow = false;

            var coords = _transform.GetMapCoordinates(xform);
            foreach (var mindEntity in _lookup.GetEntitiesInRange<MindContainerComponent>(coords, superposed.MaxObserverRange))
            {
                if (!HasComp<GhostComponent>(mindEntity.Owner) &&
                    TryComp(mindEntity.Owner, out TransformComponent? mindXform))
                {
                    var relPos = coords.Position - _transform.GetWorldPosition(mindXform);
                    if (relPos.Length() <= superposed.MinObserverRange || Vector2.Dot(_transform.GetWorldRotation(mindXform).ToWorldVec(), relPos) >= 0)
                    {
                        observedNow = true;
                        break;
                    }
                }
            }

            if (superposed.Observed != observedNow)
            {
                if (superposed.Observed)
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
}
