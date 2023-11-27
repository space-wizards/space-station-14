using Content.Shared.Chemistry.Containers.Components;
using Content.Shared.Chemistry.Solutions.EntitySystems;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private void InitializeTransfers()
    {
        SubscribeLocalEvent<RefillableSolutionComponent, DragDropDraggedEvent>(OnRefillableDragged);
    }

    private void OnRefillableDragged(EntityUid uid, RefillableSolutionComponent component, ref DragDropDraggedEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, component.Solution, out var soln, out var solution) || solution.Volume == FixedPoint2.Zero)
        {
            _popups.PopupEntity(Loc.GetString("mopping-system-empty", ("used", uid)), uid, args.User);
            return;
        }

        // Dump reagents into DumpableSolution
        if (TryComp<DumpableSolutionComponent>(args.Target, out var dump))
        {
            _solutionContainerSystem.TryGetDumpableSolution((args.Target, dump, null), out var dumpableSoln, out var dumpableSolution);
            if (dumpableSolution == null)
                return;

            bool success = true;
            if (dump.Unlimited)
            {
                var split = _solutionSystem.SplitSolution(soln, solution.Volume);
                dumpableSolution.AddSolution(split, _prototypeManager);
            }
            else
            {
                var split = _solutionSystem.SplitSolution(soln, dumpableSolution.AvailableVolume);
                success = _solutionSystem.TryAddSolution(dumpableSoln, split);
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

        // Take reagents from target
        if (!TryComp<DrainableSolutionComponent>(args.Target, out var drainable))
        {
            if (!_solutionContainerSystem.TryGetDrainableSolution((args.Target, drainable, null), out var drainableSolution, out _))
                return;

            var split = _solutionSystem.SplitSolution(drainableSolution, solution.AvailableVolume);

            if (_solutionSystem.TryAddSolution(soln, split))
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
