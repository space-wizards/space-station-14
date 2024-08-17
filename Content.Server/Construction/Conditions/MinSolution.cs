using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Server.Construction.Conditions;

/// <summary>
/// Requires that a certain solution has a minimum amount of a reagent to proceed.
/// </summary>
[DataDefinition]
public sealed partial class MinSolution : IGraphCondition
{
    /// <summary>
    /// The solution that needs to have the reagent.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;

    /// <summary>
    /// The reagent that needs to be present.
    /// </summary>
    [DataField(required: true)]
    public ReagentSpecifier Reagent = new();

    /// <summary>
    /// How much of the reagent must be present.
    /// </summary>
    [DataField]
    public FixedPoint2 Quantity = 1;

    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        var solutionSystem = entMan.System<SharedSolutionSystem>();
        if (!solutionSystem.TryGetSolution(uid, Solution, out var solution))
            return false;

        solutionSystem.TryGetReagentQuantity(solution, Reagent, out var quantity);
        return quantity >= Quantity;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var uid = args.Examined;

        var solutionSystem = entMan.System<SharedSolutionSystem>();
        if (!solutionSystem.TryGetSolution(uid, Solution, out var solution))
            return false;

        solutionSystem.TryGetReagentQuantity(solution,Reagent, out var quantity);

        // already has enough so dont show examine
        if (quantity >= Quantity)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-min-solution",
            ("quantity", Quantity - quantity), ("reagent", Name())) + "\n");
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-guide-condition-min-solution",
            Arguments = new (string, object)[]
            {
                ("quantity", Quantity),
                ("reagent", Name())
            }
        };
    }

    private string Name()
    {
        var chemRegistry = IoCManager.Resolve<IEntityManager>().System<ChemistryRegistrySystem>();
        var proto = chemRegistry.Index(Reagent.Id);
        return proto.Comp.LocalizedName;
    }
}
