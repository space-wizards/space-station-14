using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Conditions;

/// <summary>
/// Requires that a certain solution has a minimum amount of a reagent to proceed.
/// </summary>
[DataDefinition]
public sealed partial class MinSolution : GraphConditionBase<IReagentCondition>, IReagentCondition
{
    /// <summary>
    /// The solution that needs to have the reagent.
    /// </summary>
    public string? Solution { get; set; }

    /// <summary>
    /// The reagent that needs to be present.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent { get; set; } = new();

    /// <summary>
    /// How much of the reagent must be present.
    /// </summary>
    [DataField]
    public FixedPoint2 Quantity = 1;

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
        solution.TryGetReagentQuantity(new(Reagent.Id,null), out var quantity);

        // already has enough so dont show examine
        if (quantity >= Quantity)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-min-solution",
            ("quantity", Quantity - quantity), ("reagent", Name())) + "\n");
        return true;
    }

    public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
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
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var proto = protoMan.Index(Reagent);
        return proto.LocalizedName;
    }

    public FixedPoint2 Min => Quantity;

    public FixedPoint2 Max => FixedPoint2.MaxValue;
}
