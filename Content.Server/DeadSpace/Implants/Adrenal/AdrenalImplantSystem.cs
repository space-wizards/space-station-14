using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;

namespace Content.Server.DeadSpace.Implants.Adrenal;

public sealed class AdrenalImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, UseAdrenalImplantEvent>(OnAdrenalActivated);
    }

    private void OnAdrenalActivated(EntityUid uid, SubdermalImplantComponent component, UseAdrenalImplantEvent args)
    {
        if (component.ImplantedEntity is not { } target)
            return;

        var reagents = new List<(string, FixedPoint2)>()
        {
            ("Stimulants", 50f)
        };

        if (TryInjectReagents(target, reagents))
        {
            _popup.PopupEntity(Loc.GetString("adrenal-implant-activated"), target, target);
        }
        else
            return;

        args.Handled = true;
    }

    public bool TryInjectReagents(EntityUid uid, List<(string, FixedPoint2)> reagents)
    {
        var solution = new Shared.Chemistry.Components.Solution();
        foreach (var reagent in reagents)
        {
            solution.AddReagent(reagent.Item1, reagent.Item2);
        }

        if (!_solution.TryGetInjectableSolution(uid, out var targetSolution, out var _))
            return false;

        if (!_solution.TryAddSolution(targetSolution.Value, solution))
            return false;

        return true;
    }
}
