using System.Linq;
using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server.Warps;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Paper;
using Content.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Warps;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Test system until I can break them out into their own systems
/// </summary>
public sealed class TouristConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<ContainerManagerComponent> _containerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();

        SubscribeLocalEvent<StampedPapersConditionComponent, ObjectiveGetProgressEvent>(OnStampedPapersGetProgress);
        SubscribeLocalEvent<EatSpecificFoodConditionComponent, ObjectiveGetProgressEvent>(OnEatSpecificFoodGetProgress);
        SubscribeLocalEvent<EatSpecificFoodConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    // Stamps!

    private void OnStampedPapersGetProgress(Entity<StampedPapersConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = StampedPapersProgress((args.MindId, args.Mind), entity.Comp, _number.GetTarget(entity));
    }

    private void OnEatSpecificFoodGetProgress(Entity<EatSpecificFoodConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = EatSpecificFoodProgress(entity, _number.GetTarget(entity));
    }

    /// <summary>
    /// Sets the name, description and icon for the objective.
    /// </summary>
    private void OnAfterAssign(Entity<EatSpecificFoodConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var count = _number.GetTarget(condition.Owner);

        var localizedName = Loc.GetString(condition.Comp.Name);

        var description = count > 1
            ? Loc.GetString(condition.Comp.DescriptionTextMultiple, ("itemName", localizedName), ("count", count))
            : Loc.GetString(condition.Comp.DescriptionText, ("itemName", localizedName));

        _metaData.SetEntityName(condition.Owner, Loc.GetString(condition.Comp.TitleText, ("itemName", localizedName)), args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
        _objectives.SetIcon(condition.Owner, condition.Comp.Sprite, args.Objective);
    }

    private float StampedPapersProgress(Entity<MindComponent> mind, StampedPapersConditionComponent comp, float target)
    {
        if (!_containerQuery.TryGetComponent(mind.Comp.OwnedEntity, out var currentManager))
            return 0;

        var containerStack = new Stack<ContainerManagerComponent>();
        var foundStamps = new HashSet<StampDisplayInfo>();

        // recursively check each container for the item
        // checks inventory, bag, implants, etc.
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<PaperComponent>(entity, out var paper))
                        foundStamps.UnionWith(paper.StampedBy.ToHashSet());

                    // if it is a container check its contents
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack.Push(containerManager);
                }
            }
        } while (containerStack.TryPop(out currentManager));

        var result = foundStamps.Count / target;
        result = Math.Clamp(result, 0, 1);
        return result;
    }

    private float EatSpecificFoodProgress(EatSpecificFoodConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.FoodEaten / (float) target, 1f);
    }
}
