using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids;

public sealed partial class SpillActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpillableComponent, SpillActionEvent>(OnSpillAction);
    }

    private void OnSpillAction(EntityUid uid, SpillableComponent comp, SpillActionEvent args)
    {
        if (!_solutionContainerSystem.TryGetDrainableSolution(uid, out var soln, out var solution) || solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainerSystem.SplitSolution(soln.Value, solution.Volume);
        _puddleSystem.TrySpillAt(uid, puddleSolution, out _);
        _popupSystem.PopupClient(Loc.GetString("spill-action-use", ("name", uid)), uid, uid);
    }

    public sealed partial class SpillActionEvent : InstantActionEvent { }
}
