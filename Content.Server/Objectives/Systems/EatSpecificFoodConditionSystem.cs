using Content.Server.Objectives.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Test system until I can break them out into their own systems
/// </summary>
public sealed class EatSpecificFoodConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EatSpecificFoodConditionComponent, ObjectiveGetProgressEvent>(OnEatSpecificFoodGetProgress);
        SubscribeLocalEvent<EatSpecificFoodConditionComponent, ObjectiveAfterAssignEvent>(OnEatSpecificFoodAfterAssign);
    }

    private void OnEatSpecificFoodGetProgress(Entity<EatSpecificFoodConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = EatSpecificFoodProgress(entity, _number.GetTarget(entity));
    }

    /// <summary>
    /// Sets the name, description and icon for the objective.
    /// </summary>
    private void OnEatSpecificFoodAfterAssign(Entity<EatSpecificFoodConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
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

    private float EatSpecificFoodProgress(EatSpecificFoodConditionComponent comp, int target)
    {
        // Prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.FoodEaten / (float) target, 1f);
    }
}
