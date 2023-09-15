using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class SolutionContainerSystem
{
    public void Refill(EntityUid targetUid, Solution targetSolution, Solution addedSolution,
        RefillableSolutionComponent? refillableSolution = null)
    {
        if (!Resolve(targetUid, ref refillableSolution, false))
            return;

        TryAddSolution(targetUid, targetSolution, addedSolution);
    }

    public void Inject(EntityUid targetUid, Solution targetSolution, Solution addedSolution,
        InjectableSolutionComponent? injectableSolution = null)
    {
        if (!Resolve(targetUid, ref injectableSolution, false))
            return;

        TryAddSolution(targetUid, targetSolution, addedSolution);
    }

    public Solution Draw(EntityUid targetUid, Solution solution, FixedPoint2 amount,
        DrawableSolutionComponent? drawableSolution = null)
    {
        if (!Resolve(targetUid, ref drawableSolution, false))
            return new Solution();

        return SplitSolution(targetUid, solution, amount);
    }

    public Solution Drain(EntityUid targetUid, Solution targetSolution, FixedPoint2 amount,
        DrainableSolutionComponent? drainableSolution = null)
    {
        if (!Resolve(targetUid, ref drainableSolution, false))
            return new Solution();

        return SplitSolution(targetUid, targetSolution, amount);
    }

    public bool TryGetInjectableSolution(EntityUid targetUid,
        [NotNullWhen(true)] out Solution? solution,
        InjectableSolutionComponent? injectable = null,
        SolutionContainerManagerComponent? manager = null
    )
    {
        if (!Resolve(targetUid, ref manager, ref injectable, false)
            || !manager.Solutions.TryGetValue(injectable.Solution, out solution))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetRefillableSolution(EntityUid targetUid,
        [NotNullWhen(true)] out Solution? solution,
        SolutionContainerManagerComponent? solutionManager = null,
        RefillableSolutionComponent? refillable = null)
    {
        if (!Resolve(targetUid, ref solutionManager, ref refillable, false)
            || !solutionManager.Solutions.TryGetValue(refillable.Solution, out var refillableSolution))
        {
            solution = null;
            return false;
        }

        solution = refillableSolution;
        return true;
    }

    public bool TryGetDrainableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        DrainableSolutionComponent? drainable = null,
        SolutionContainerManagerComponent? manager = null)
    {
        if (!Resolve(uid, ref drainable, ref manager, false)
            || !manager.Solutions.TryGetValue(drainable.Solution, out solution))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetDumpableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        DumpableSolutionComponent? dumpable = null,
        SolutionContainerManagerComponent? manager = null)
    {
        if (!Resolve(uid, ref dumpable, ref manager, false)
            || !manager.Solutions.TryGetValue(dumpable.Solution, out solution))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetDrawableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        DrawableSolutionComponent? drawable = null,
        SolutionContainerManagerComponent? manager = null)
    {
        if (!Resolve(uid, ref drawable, ref manager, false)
            || !manager.Solutions.TryGetValue(drawable.Solution, out solution))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public FixedPoint2 DrainAvailable(EntityUid uid)
    {
        return !TryGetDrainableSolution(uid, out var solution)
            ? FixedPoint2.Zero
            : solution.Volume;
    }

    public float PercentFull(EntityUid uid)
    {
        if (!TryGetDrainableSolution(uid, out var solution) || solution.MaxVolume.Equals(FixedPoint2.Zero))
            return 0;

        return solution.FillFraction * 100;
    }

    public bool TryGetFitsInDispenser(EntityUid owner,
        [NotNullWhen(true)] out Solution? solution,
        FitsInDispenserComponent? dispenserFits = null,
        SolutionContainerManagerComponent? solutionManager = null)
    {
        if (!Resolve(owner, ref dispenserFits, ref solutionManager, false)
            || !solutionManager.Solutions.TryGetValue(dispenserFits.Solution, out solution))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public static string ToPrettyString(Solution solution)
    {
        var sb = new StringBuilder();
        if (solution.Name == null)
            sb.Append("[");
        else
            sb.Append($"{solution.Name}:[");
        var first = true;
        foreach (var (id, quantity) in solution.Contents)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.Append(", ");
            }

            sb.AppendFormat("{0}: {1}u", id, quantity);
        }

        sb.Append(']');
        return sb.ToString();
    }
}
