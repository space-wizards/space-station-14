using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private void InitializeTransfers()
    {
        SubscribeLocalEvent<RefillableSolutionComponent, DragDropDraggedEvent>(OnRefillableDragged);
    }

    private void OnRefillableDragged(EntityUid uid, RefillableSolutionComponent component, ref DragDropDraggedEvent args)
    {
        _solutionContainerSystem.TryGetSolution(uid, component.Solution, out var solution);

        if (solution?.Volume == FixedPoint2.Zero)
        {
            _popups.PopupEntity(Loc.GetString("mopping-system-empty", ("used", uid)), uid, args.User);
            return;
        }

        // Dump reagents into DumpableSolution
        if (TryComp<DumpableSolutionComponent>(args.Target, out var dump))
        {
            _solutionContainerSystem.TryGetDumpableSolution(args.Target, out var dumpableSolution, dump);
            if (dumpableSolution == null || solution == null)
                return;

            bool success = true;
            if (dump.Unlimited)
            {
                var split = _solutionContainerSystem.SplitSolution(uid, solution, solution.Volume);
                dumpableSolution.AddSolution(split, _prototypeManager);
            }
            else
            {
                var split = _solutionContainerSystem.SplitSolution(uid, solution, dumpableSolution.AvailableVolume);
                success = _solutionContainerSystem.TryAddSolution(args.Target, dumpableSolution, split);
            }

            if (success)
            {
                _audio.PlayPvs(AbsorbentComponent.DefaultTransferSound, args.Target);
            }
            else
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-full", ("used", args.Target)), args.Target, args.User);
            }

            return;
        }

        TryComp<DrainableSolutionComponent>(args.Target, out var drainable);

        _solutionContainerSystem.TryGetDrainableSolution(args.Target, out var drainableSolution, drainable);

        // Take reagents from target
        if (drainable != null)
        {
            if (drainableSolution == null || solution == null)
                return;

            var split = _solutionContainerSystem.SplitSolution(args.Target, drainableSolution, solution.AvailableVolume);

            if (_solutionContainerSystem.TryAddSolution(uid, solution, split))
            {
                _audio.PlayPvs(AbsorbentComponent.DefaultTransferSound, uid);
            }
            else
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-full", ("used", uid)), uid, args.User);
            }
        }
    }
}
