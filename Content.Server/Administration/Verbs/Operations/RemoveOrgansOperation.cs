using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnRemoveOrgans(Entity<BodyComponent> entity, ref AdminOperationEvent<RemoveOrgansOperation> args)
    {
        if (args.Operation.MaxCount is <= 0)
            return;

        var selected = new List<EntityUid>();
        foreach (var organ in _body.EnumerateOrgans<TransformComponent>(entity.AsNullable()))
        {
            var category = organ.Comp1.Category;
            if (args.Operation.Categories != null &&
                (category == null || !args.Operation.Categories.Contains(category.Value)))
                continue;

            if (category != null && args.Operation.ExcludedCategories.Contains(category.Value))
                continue;

            selected.Add(organ);
            if (selected.Count == args.Operation.MaxCount)
                break;
        }

        foreach (var organ in selected)
        {
            if (args.Operation.Delete)
                QueueDel(organ);
            else
                _transform.AttachToGridOrMap(organ);
        }
    }
}

/// <summary>
/// Removes matching organs from a body. By default, removed organs are detached into the world.
/// </summary>
public sealed partial class RemoveOrgansOperation : AdminOperationBase<RemoveOrgansOperation>
{
    /// <summary>
    /// If null, organs from any category may be removed.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>>? Categories { get; private set; }

    /// <summary>
    /// Exclusions take precedence over <see cref="Categories"/>.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExcludedCategories { get; private set; } = [];

    /// <summary>
    /// Deletes selected organs instead of detaching them.
    /// </summary>
    [DataField]
    public bool Delete { get; private set; }

    /// <summary>
    /// Null removes every match. Values less than or equal to zero remove nothing.
    /// </summary>
    [DataField]
    public int? MaxCount { get; private set; }
}
