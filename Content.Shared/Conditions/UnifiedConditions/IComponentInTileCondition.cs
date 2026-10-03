using Content.Shared.Maps;
using Robust.Shared.Map.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IComponentInTileCondition : ICondition
{
    string Component { get; }
}

public sealed partial class ComponentInTileConditionSystem : ConditionEvaluatorSystem<IComponentInTileCondition>
{
    [Dependency] private IComponentFactory ComponentFactory = default!;
    [Dependency] private IEntityManager entityManager = default!;
    [Dependency] private EntityLookupSystem lookup = default!;
    [Dependency] private SharedTransformSystem transformSys = default!;

    public override float Evaluate(IComponentInTileCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (string.IsNullOrEmpty(condition.Component))
            return 0;

        if (!TryComp(entityUid, out TransformComponent? transform))
            return 0;

        if (transform.GridUid == null)
            return 0;

        if (!entityManager.TryGetComponent<MapGridComponent>(transform.GridUid.Value, out var grid))
            return 0;

        var type = ComponentFactory.GetRegistration(condition.Component).Type;

        var indices = transform.Coordinates.ToVector2i(entityManager, transformSys);

        if (!entityManager.System<SharedMapSystem>()
                .TryGetTileRef(transform.GridUid.Value, grid, indices, out var tile))
            return 0;
        float value = 0;
        foreach (var ent in lookup.GetEntitiesInTile(tile, LookupFlags.Approximate | LookupFlags.Static))
        {
            if (entityManager.HasComponent(ent, type))
                value++;
        }

        return value;
    }
}
