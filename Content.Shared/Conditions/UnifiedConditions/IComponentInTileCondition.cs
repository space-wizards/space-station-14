using Content.Shared.Maps;
using Robust.Shared.Map.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IComponentInTileCondition : ICondition<IComponentInTileCondition>
{
    string Component { get; }
}

public sealed partial class ComponentInTileConditionSystem : EntitySystem
{

    [Dependency] private IComponentFactory ComponentFactory = default!;
    [Dependency] private SharedTransformSystem transformSys = default!;
    [Dependency] private EntityLookupSystem lookup = default!;
    [Dependency] private IEntityManager entityManager = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity,
        ref ConditionEvaluationEvent<IComponentInTileCondition> args)
    {
        args.Handled = true;

        if (string.IsNullOrEmpty(args.Condition.Component))
            return;

        if (entity.Comp.GridUid == null)
            return;

        var transform = entity.Comp;

        if (!entityManager.TryGetComponent<MapGridComponent>(transform.GridUid.Value, out var grid))
            return;

        var type = ComponentFactory.GetRegistration(args.Condition.Component).Type;

        var indices = transform.Coordinates.ToVector2i(entityManager, transformSys);

        if (!entityManager.System<SharedMapSystem>().TryGetTileRef(transform.GridUid.Value, grid, indices, out var tile))
            return;

        foreach (var ent in lookup.GetEntitiesInTile(tile, flags: LookupFlags.Approximate | LookupFlags.Static))
        {
            if (entityManager.HasComponent(ent, type))
                args.Value++;
        }
    }
}
