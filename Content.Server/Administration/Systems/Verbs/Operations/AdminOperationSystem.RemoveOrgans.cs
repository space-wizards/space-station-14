using Content.Server.Administration.Verbs.Operations;
using Content.Server.Administration.Verbs.Operations.Smites;
using Content.Shared.Body;

namespace Content.Server.Administration.Systems.Verbs.Operations;

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
