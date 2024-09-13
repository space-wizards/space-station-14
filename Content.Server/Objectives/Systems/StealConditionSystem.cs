using Content.Server.Objectives.Components;
using Content.Server.Objectives.Components.Targets;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stacks;

namespace Content.Server.Objectives.Systems;

public sealed class StealConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<ContainerManagerComponent> _containerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();

        SubscribeLocalEvent<StealConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StealConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    /// start checks of target acceptability, and generation of start values.
    private void OnAssigned(Entity<StealConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        List<StealTargetComponent?> targetList = new();

        var query = AllEntityQuery<StealTargetComponent>();
        while (query.MoveNext(out var target))
        {
            if (condition.Comp.StealGroup != target.StealGroup)
                continue;

            targetList.Add(target);
        }

        // cancel if the required items do not exist
        if (targetList.Count == 0 && condition.Comp.VerifyMapExistence)
        {
            args.Cancelled = true;
            return;
        }

        //setup condition settings
        var maxSize = condition.Comp.VerifyMapExistence
            ? Math.Min(targetList.Count, condition.Comp.MaxCollectionSize)
            : condition.Comp.MaxCollectionSize;
        var minSize = condition.Comp.VerifyMapExistence
            ? Math.Min(targetList.Count, condition.Comp.MinCollectionSize)
            : condition.Comp.MinCollectionSize;

        condition.Comp.CollectionSize = _random.Next(minSize, maxSize);
    }

    //Set the visual, name, icon for the objective.
    private void OnAfterAssign(Entity<StealConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var group = _proto.Index(condition.Comp.StealGroup);

        var title =condition.Comp.OwnerText == null
            ? Loc.GetString(condition.Comp.ObjectiveNoOwnerText, ("itemName", group.Name))
            : Loc.GetString(condition.Comp.ObjectiveText, ("owner", Loc.GetString(condition.Comp.OwnerText)), ("itemName", group.Name));

        var description = condition.Comp.CollectionSize > 1
            ? Loc.GetString(condition.Comp.DescriptionMultiplyText, ("itemName", group.Name), ("count", condition.Comp.CollectionSize))
            : Loc.GetString(condition.Comp.DescriptionText, ("itemName", group.Name));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
        _objectives.SetIcon(condition.Owner, group.Sprite, args.Objective);
    }
    private void OnGetProgress(Entity<StealConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, condition);
    }

    private float GetProgress(MindComponent mind, StealConditionComponent condition)
    {
        if (!_containerQuery.TryGetComponent(mind.OwnedEntity, out var currentManager))
            return 0;

        var containerStack = new Stack<ContainerManagerComponent>();
        var count = 0;

        //check stealAreas
        if (condition.CheckStealAreas)
        {
            var areasQuery = AllEntityQuery<StealAreaComponent>();
            while (areasQuery.MoveNext(out var uid, out var area))
            {
                if (!area.Owners.Contains(mind.Owner))
                    continue;

                var nearestEnt = _lookup.GetEntitiesInRange(uid, area.Range);
                foreach (var ent in nearestEnt)
                {
                    CheckEntity(ent, condition, ref containerStack, ref count);
                }
            }
        }

        //check pulling object
        if (TryComp<PullerComponent>(mind.OwnedEntity, out var pull)) //TO DO: to make the code prettier? don't like the repetition
        {
            var pulledEntity = pull.Pulling;
            if (pulledEntity != null)
            {
                CheckEntity(pulledEntity.Value, condition, ref containerStack, ref count);
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
                    count += CheckStealTarget(entity, condition);

                    // if it is a container check its contents
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack.Push(containerManager);
                }
            }
        } while (containerStack.TryPop(out currentManager));

        var result = count / (float) condition.CollectionSize;
        result = Math.Clamp(result, 0, 1);
        return result;
    }

    private void CheckEntity(EntityUid entity, StealConditionComponent condition, ref Stack<ContainerManagerComponent> containerStack, ref int counter)
    {
        // check if this is the item
        counter += CheckStealTarget(entity, condition);

        //we don't check the inventories of sentient entity
        if (!TryComp<MindContainerComponent>(entity, out var pullMind))
        {
            // if it is a container check its contents
            if (_containerQuery.TryGetComponent(entity, out var containerManager))
                containerStack.Push(containerManager);
        }
    }

    private int CheckStealTarget(EntityUid entity, StealConditionComponent condition)
    {
        // check if this is the target
        if (!TryComp<StealTargetComponent>(entity, out var target))
            return 0;

        if (target.StealGroup != condition.StealGroup)
            return 0;

        // check if needed target alive
        if (condition.CheckAlive)
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (!_mobState.IsAlive(entity, state))
                    return 0;
            }
        }

        return TryComp<StackComponent>(entity, out var stack) ? stack.Count : 1;
    }
}
