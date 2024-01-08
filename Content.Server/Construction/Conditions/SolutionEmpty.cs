using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Examine;

namespace Content.Server.Construction.Conditions;

/// <summary>
/// Requires that a certain solution be empty to proceed.
/// </summary>
[DataDefinition]
public sealed partial class SolutionEmpty : IGraphCondition
{
    /// <summary>
    /// The solution that needs to be empty.
    /// </summary>
    [DataField]
    public string Solution;

    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        var containerSys = entMan.System<SolutionContainerSystem>();
        if (!containerSys.TryGetSolution(uid, Solution, out _, out var solution))
            return false;

        return solution.Volume == 0;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var uid = args.Examined;

        var containerSys = entMan.System<SolutionContainerSystem>();
        if (!containerSys.TryGetSolution(uid, Solution, out _, out var solution))
            return false;

        // already empty so dont show examine
        if (solution.Volume == 0)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-solution-empty"));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-guide-condition-solution-empty"
        };
    }
}
