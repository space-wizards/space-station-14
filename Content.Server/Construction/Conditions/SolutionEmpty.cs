using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Server.Construction.Conditions;

/// <summary>
/// Requires that a certain solution be empty to proceed.
/// </summary>
[DataDefinition]
public sealed partial class SolutionEmpty : GraphConditionBase<ISolutionVolumeCondition>, ISolutionVolumeCondition
{
    /// <summary>
    /// The solution that needs to be empty.
    /// </summary>
    [DataField]
    public string? Solution { get; set; }

    public override bool DoExamine(ExaminedEvent args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var uid = args.Examined;

        Solution? solution;
        if (Solution == null)
        {
            if (!entMan.TryGetComponent(uid, out SolutionComponent? solutionComponent))
                return false;
            solution = solutionComponent.Solution;
        }
        else
        {
            var containerSys = entMan.System<SharedSolutionContainerSystem>();
            if (!containerSys.TryGetSolution(uid, Solution, out _, out solution))
                return false;
        }
        // already empty so dont show examine
        if (solution.Volume == 0)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-solution-empty"));
        return true;
    }

    public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-guide-condition-solution-empty"
        };
    }

    public FixedPoint2 Min => 0;

    public FixedPoint2 Max => 0;
}
