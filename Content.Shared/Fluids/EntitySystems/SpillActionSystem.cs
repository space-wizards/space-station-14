using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids;

public sealed partial class SpillActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpillableComponent, SpillActionEvent>(OnSpillAction);
    }

    private void OnSpillAction(Entity<SpillableComponent> ent, ref SpillActionEvent args)
    {
        if (!_solutionContainer.TryGetDrainableSolution(ent.Owner, out var soln, out var solution) || solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainer.SplitSolution(soln.Value, solution.Volume);
        _puddle.TrySpillAt(ent, puddleSolution, out _);
        _popup.PopupClient(Loc.GetString("spill-action-use", ("name", ent)), ent, ent);
    }
}

public sealed partial class SpillActionEvent : InstantActionEvent;
