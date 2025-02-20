using Content.Shared.Chemistry.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Handles solution transfer when a beaker is used on a scoopable entity.
/// </summary>
public sealed class ScoopableSolutionSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScoopableSolutionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<ScoopableSolutionComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryScoop(ent, args.Used, args.User);
    }

    public bool TryScoop(Entity<ScoopableSolutionComponent> ent, EntityUid beaker, EntityUid user)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var src, out var srcSolution) ||
            !_solution.TryGetRefillableSolution(beaker, out var target, out _))
            return false;

        var scooped = _solutionTransfer.Transfer(user, ent, src.Value, beaker, target.Value, srcSolution.Volume);
        if (scooped == 0)
            return false;

        _popup.PopupClient(Loc.GetString(ent.Comp.Popup, ("scooped", ent.Owner), ("beaker", beaker)), user, user);

        if (srcSolution.Volume == 0 && ent.Comp.Delete)
        {
            // deletion isnt predicted so do this to prevent spam clicking to see "the ash is empty!"
            RemCompDeferred<ScoopableSolutionComponent>(ent);

            if (!_netManager.IsClient)
                QueueDel(ent);
        }

        return true;
    }
}
