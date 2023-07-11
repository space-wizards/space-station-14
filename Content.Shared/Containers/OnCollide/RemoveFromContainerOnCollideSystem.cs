using System.Linq;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Shared.Containers.OnCollide;

public sealed class RemoveFromContainerOnCollideSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoveFromContainerOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, RemoveFromContainerOnCollideComponent component, ref StartCollideEvent args)
    {
        var currentVelocity = args.OurBody.LinearVelocity.Length();
        if (currentVelocity < component.RequiredVelocity)
            return;

        if (!_containerSystem.TryGetContainer(uid, component.Container, out var container))
            return;

        if (component.CollidableEntities != null && component.CollidableEntities.IsValid(args.OtherEntity, EntityManager))
            return;

        var toRemove = container.ContainedEntities.ToList();
        // add strapped/buckled entities to the toRemove list if allowed and unbuckle
        if (component.RemoveStrapped && TryComp<StrapComponent>(uid, out var strapComponent)
           && strapComponent.BuckledEntities.Count != 0)
        {
            foreach (var buckled in strapComponent.BuckledEntities)
            {
                _buckleSystem.TryUnbuckle(buckled, buckled, true);
                toRemove.Add(buckled);
            }
        }

        if (toRemove.Count == 0)
            return;

        // remove, paralyze and throw randomly everything in toRemove
        foreach (var removing in toRemove)
        {
            container.Remove(removing, EntityManager);

            if (component.EjectAfterRemove)
            {
                var randomOffset = _random.NextAngle(component.EjectRange.Min, component.EjectRange.Max).ToVec();
                var direction = randomOffset * Transform(uid).Coordinates.Position;
                _throwingSystem.TryThrow(removing, direction, component.EjectStrength, uid, component.EjectPushbackRatio);
            }

            if (!component.RemoveEverything)
                break;
        }
    }
}
