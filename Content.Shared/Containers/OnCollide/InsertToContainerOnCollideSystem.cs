using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Containers.OnCollide;

public sealed class InsertToContainerOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsertToContainerOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, InsertToContainerOnCollideComponent component, ref StartCollideEvent args)
    {
        var currentVelocity = args.OurBody.LinearVelocity.Length();
        if (currentVelocity < component.RequiredVelocity)
            return;

        if (!_containerSystem.TryGetContainer(uid, component.Container, out var container))
            return;

        if (component.BlacklistedEntities != null && component.BlacklistedEntities.IsValid(args.OtherEntity, EntityManager))
            return;

        if (component.InsertableEntities != null && !component.InsertableEntities.IsValid(args.OtherEntity, EntityManager))
            return;

        container.Insert(args.OtherEntity, EntityManager, physics: args.OtherBody);
    }
}
