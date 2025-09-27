using Content.Server.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Disease;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Disease.Cures;

[DataDefinition]
public sealed partial class CureReagent : CureStep
{
    /// <summary>
    /// List of required reagents (all must be present at or above the required quantity).
    /// </summary>
    [DataField(required: true)]
    public List<Requirement> Requirements { get; private set; } = new();

    [DataDefinition]
    public sealed partial class Requirement
    {
        [DataField(required: true)] public string ReagentId { get; private set; } = string.Empty;
        [DataField] public FixedPoint2 Quantity { get; private set; } = FixedPoint2.New(1);
    }
}

public sealed partial class CureReagent
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    /// <summary>
    /// Cures the disease if the bloodstream has the required reagents. Does not consume them.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseasePrototype disease)
    {
        if (!_entityManager.TryGetComponent(uid, out BloodstreamComponent? bloodstream))
            return false;

        if (!_solutions.ResolveSolution(uid, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution, out var chemSolution))
            return false;

        if (Requirements.Count == 0)
            return false;

        foreach (var req in Requirements)
        {
            var have = chemSolution.GetTotalPrototypeQuantity(req.ReagentId);
            if (have < req.Quantity)
                return false;
        }

        return true;
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        var parts = new List<string>();
        foreach (var r in Requirements)
        {
            var name = r.ReagentId;
            if (prototypes.TryIndex<ReagentPrototype>(r.ReagentId, out var proto))
                name = proto.LocalizedName;

            parts.Add(Loc.GetString("diagnoser-cure-reagent-item", ("units", r.Quantity.ToString()), ("reagent", name)));
        }

        var joined = string.Join(", ", parts);
        yield return Loc.GetString("diagnoser-cure-reagents-all", ("list", joined));
    }
}
