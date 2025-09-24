using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Disease;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomAdjustReagent : SymptomBehavior
{
    /// <summary>
    /// List of reagent adjustments to apply. Quantity > 0 adds, < 0 removes.
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

public sealed partial class SymptomAdjustReagent
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    /// Adjust the carrier's internal chemical solution by the configured amount for the configured reagent.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        if (!_entMan.TryGetComponent(uid, out BloodstreamComponent? bloodstream))
            return;

        if (!_solutions.ResolveSolution(uid, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution, out _))
            return;

        if (Requirements.Count == 0)
            return;

        var sol = bloodstream.ChemicalSolution!.Value;
        foreach (var req in Requirements)
        {
            if (req.Quantity == FixedPoint2.Zero)
                continue;

            if (req.Quantity > FixedPoint2.Zero)
                _solutions.TryAddReagent(sol, req.ReagentId, req.Quantity, out _);
            else
                _solutions.RemoveReagent(sol, req.ReagentId, -req.Quantity);
        }
    }
}


