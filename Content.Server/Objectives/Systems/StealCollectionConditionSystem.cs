using Content.Server.Objectives.Components;
using Content.Server.Objectives.Components.Targets;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Content.Shared.Pulling.Components;

namespace Content.Server.Objectives.Systems;

public sealed class StealCollectionConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    private EntityQuery<ContainerManagerComponent> containerQuery;
    private EntityQuery<MetaDataComponent> metaQuery;

    public override void Initialize()
    {
        base.Initialize();

        containerQuery = GetEntityQuery<ContainerManagerComponent>();
        metaQuery = GetEntityQuery<MetaDataComponent>();

        SubscribeLocalEvent<StealCollectionConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealCollectionConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StealCollectionConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }
    private void OnAssigned(Entity<StealCollectionConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        List<StealCollectionTargetComponent?> targetList = new();

        // cancel if invalid TargetStealName
        var group = _proto.Index<StealTargetGroupPrototype>(condition.Comp.StealGroup);
        if (group == null)
        {
            args.Cancelled = true;
            Log.Error("StealTargetGroup invalid prototype!");
            return;
        }

        var query = EntityQueryEnumerator<StealCollectionTargetComponent>();
        while (query.MoveNext(out var uid, out var target))
        {
            if (condition.Comp.StealGroup != target.StealGroup)
                continue;

            targetList.Add(target);
        }

        // cancel if the required items do not exist
        if (targetList.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        //setup condition settings
        var maxSize = Math.Min(targetList.Count, condition.Comp.MaxCollectionSize);
        var minSize = Math.Min(targetList.Count, condition.Comp.MinCollectionSize);
        condition.Comp.CollectionSize = _random.Next(minSize, maxSize);
    }

    private void OnAfterAssign(Entity<StealCollectionConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        //установить иконку, описание и название цели
        var group = _proto.Index<StealTargetGroupPrototype>(condition.Comp.StealGroup);

        var title = Loc.GetString(condition.Comp.ObjectiveText, ("itemName", group.Name));

        var description = condition.Comp.CollectionSize > 1
            ? Loc.GetString(condition.Comp.DescriptionMultiplyText, ("itemName", group.Name), ("count", condition.Comp.CollectionSize))
            : Loc.GetString(condition.Comp.DescriptionText, ("itemName", group.Name));

        //group.Name;
        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
        _objectives.SetIcon(condition.Owner, group.Sprite, args.Objective);
    }

    private void OnGetProgress(Entity<StealCollectionConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, condition);
    }

    private float GetProgress(MindComponent mind, StealCollectionConditionComponent condition)
    {
        if (!metaQuery.TryGetComponent(mind.OwnedEntity, out var meta))
            return 0;
        if (!containerQuery.TryGetComponent(mind.OwnedEntity, out var currentManager))
            return 0;

        var stack = new Stack<ContainerManagerComponent>();
        var count = 0;

        //check pulling object
        if (TryComp<SharedPullerComponent>(mind.OwnedEntity, out var pull))
        {
            var pullid = pull.Pulling;
            if (pullid != null)
            {
                // check if this is the item
                if (TryComp<StealCollectionTargetComponent>(pullid, out var target))
                    if (target.StealGroup == condition.StealGroup) count++;

                // if it is a container check its contents
                if (containerQuery.TryGetComponent(pullid, out var containerManager))
                    stack.Push(containerManager);
            }
        }

        // recursively check each container for the item
        // checks inventory, bag, implants, etc.
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    // check if this is the item
                    if (TryComp<StealCollectionTargetComponent>(entity, out var target))
                        if (target.StealGroup == condition.StealGroup) count++;

                    // if it is a container check its contents
                    if (containerQuery.TryGetComponent(entity, out var containerManager))
                        stack.Push(containerManager);
                }
            }
        } while (stack.TryPop(out currentManager));

        

            var result = (float) count / (float) condition.CollectionSize;
        result = Math.Clamp(result, 0, 1);
        return result;
    }
}
