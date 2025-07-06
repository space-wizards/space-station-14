using Content.Shared.Chemistry.Components;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    private void InitializeTransfers()
    {
        SubscribeLocalEvent<RefillableSolutionComponent, DragDropDraggedEvent>(OnRefillableDragged);
    }

    private void OnRefillableDragged(Entity<RefillableSolutionComponent> entity, ref DragDropDraggedEvent args)
    {
        if (!_actionBlocker.CanComplexInteract(args.User))
        {
            Popups.PopupClient(Loc.GetString("mopping-system-no-hands"), args.User, args.User);
            return;
        }

        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.Solution, out var soln, out var solution)
            || solution.Volume == FixedPoint2.Zero)
        {
            Popups.PopupClient(Loc.GetString("mopping-system-empty", ("used", entity.Owner)), entity, args.User);
            return;
        }

        // Dump reagents into DumpableSolution
        if (TryComp<DumpableSolutionComponent>(args.Target, out var dump))
        {
            if (!_solutionContainerSystem.TryGetDumpableSolution((args.Target, dump, null),
                    out var dumpableSoln,
                    out var dumpableSolution))
                return;

            if (!_solutionContainerSystem.TryGetDrainableSolution(entity.Owner, out _, out _))
                return;

            if (Openable.IsClosed(entity))
                return;

            var success = true;
            if (dump.Unlimited)
            {
                dumpableSolution.AddSolution(_solutionContainerSystem.SplitSolution(soln.Value, solution.Volume),
                    _prototypeManager);
            }
            else
            {
                success = _solutionContainerSystem.TryAddSolution(dumpableSoln.Value,
                    _solutionContainerSystem.SplitSolution(soln.Value, dumpableSolution.AvailableVolume));
            }

            if (success)
                Audio.PlayPredicted(AbsorbentComponent.DefaultTransferSound, args.Target, args.User);
            else
                Popups.PopupClient(Loc.GetString("mopping-system-full", ("used", args.Target)), args.Target, args.User);

            return;
        }

        // Take reagents from target
        if (TryComp<DrainableSolutionComponent>(args.Target, out var drainable))
            return;

        if (!_solutionContainerSystem.TryGetDrainableSolution((args.Target, drainable, null),
                out var drainableSolution,
                out _))
            return;

        var split = _solutionContainerSystem.SplitSolution(drainableSolution.Value, solution.AvailableVolume);

        if (_solutionContainerSystem.TryAddSolution(soln.Value, split))
            Audio.PlayPredicted(AbsorbentComponent.DefaultTransferSound, entity, args.User);
        else
            Popups.PopupClient(Loc.GetString("mopping-system-full", ("used", entity.Owner)), entity, args.User);
    }
}
