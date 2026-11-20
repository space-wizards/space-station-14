using Content.Shared.Body;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Drops or deletes organs matching the category and entity filters.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class RemoveOrgansEntityEffectSystem : EntityEffectSystem<BodyComponent, RemoveOrgans>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

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
            if (!_whitelist.CheckBoth(organ, args.Effect.Blacklist, args.Effect.Whitelist))
                continue;

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

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class RemoveOrgans : EntityEffectBase<RemoveOrgans>
{
    /// <summary>
    /// Categories to remove. Null allows any category, including uncategorized organs.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>>? Categories;

    /// <summary>
    /// Categories to keep, even if included in Categories.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExcludedCategories = [];

    /// <summary>
    /// Additional filter for the organs, not the body.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Organs to keep regardless of the other filters.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Delete selected organs instead of dropping them.
    /// </summary>
    [DataField]
    public bool Delete;

    /// <summary>
    /// Maximum number to remove in body enumeration order. Null removes all matches; zero or less removes none.
    /// </summary>
    [DataField]
    public int? MaxCount;
}
