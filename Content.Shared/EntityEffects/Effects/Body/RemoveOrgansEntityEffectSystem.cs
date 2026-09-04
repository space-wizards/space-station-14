using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class RemoveOrgansEntityEffectSystem : EntityEffectSystem<BodyComponent, RemoveOrgans>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<BodyComponent> entity, ref EntityEffectEvent<RemoveOrgans> args)
    {
        var categories = args.Effect.Categories;
        var excludedCategories = args.Effect.ExcludedCategories;
        var maxCount = args.Effect.MaxCount;
        var delete = args.Effect.Delete;

        if (maxCount is <= 0)
            return;

        var selected = new List<EntityUid>();
        foreach (var organ in _body.EnumerateOrgans<TransformComponent>(entity.AsNullable()))
        {
            var category = organ.Comp1.Category;
            if (categories != null && (category == null || !categories.Contains(category.Value)))
                continue;

            if (category != null && excludedCategories.Contains(category.Value))
                continue;

            selected.Add(organ);
            if (selected.Count == maxCount)
                break;
        }

        foreach (var organ in selected)
        {
            if (delete)
                QueueDel(organ);
            else
                _transform.AttachToGridOrMap(organ);
        }
    }
}

public sealed partial class RemoveOrgans : EntityEffectBase<RemoveOrgans>
{
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>>? Categories;

    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExcludedCategories = [];

    [DataField]
    public bool Delete;

    [DataField]
    public int? MaxCount;
}
