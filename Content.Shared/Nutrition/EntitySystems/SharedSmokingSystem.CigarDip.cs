using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedSmokingSystem
{
    private bool TryDipCigar(Entity<CigarComponent> entity, SmokableComponent smokable, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return false;

        if (!TryComp(target, out DrainableSolutionComponent? drainable))
            return false;

        if (_openable.IsClosed(target, args.User, predicted: true))
            return true;

        if (!_solutionContainerSystem.TryGetSolution(target, drainable.Solution, out var containerSoln, out var containerSolution))
            return false;

        if (containerSolution.Volume <= FixedPoint2.Zero)
        {
            _popupSystem.PopupClient(Loc.GetString("cigar-component-dip-empty", ("cigar", entity.Owner)), target, args.User);
            return true;
        }

        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, smokable.Solution, out var cigSoln, out var cigSolution))
            return false;

        if (cigSolution.Volume >= cigSolution.MaxVolume)
        {
            _popupSystem.PopupClient(Loc.GetString("cigar-component-dip-full", ("cigar", entity.Owner)), target, args.User);
            return true;
        }

        var amountToDrain = FixedPoint2.Min(cigSolution.AvailableVolume, entity.Comp.DipAmount);

        var drawn = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToDrain);

        _solutionContainerSystem.TryAddSolution(cigSoln.Value, drawn);

        _popupSystem.PopupClient(Loc.GetString("cigar-component-dip-success", ("cigar", entity.Owner), ("target", target)), target, args.User);

        return true;
    }
}
