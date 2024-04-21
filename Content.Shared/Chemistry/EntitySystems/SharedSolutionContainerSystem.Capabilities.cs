using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Content.Shared.Chemistry.EntitySystems;

public abstract partial class SharedSolutionContainerSystem
{
    #region Solution Accessors

    public bool TryGetRefillableSolution(Entity<RefillableSolutionComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    public bool TryGetDrainableSolution(Entity<DrainableSolutionComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    public bool TryGetDumpableSolution(Entity<DumpableSolutionComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    public bool TryGetDrawableSolution(Entity<DrawableSolutionComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    public bool TryGetInjectableSolution(Entity<InjectableSolutionComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    public bool TryGetFitsInDispenser(Entity<FitsInDispenserComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    public bool TryGetMixableSolution(Entity<MixableSolutionComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    #endregion Solution Accessors

    #region Solution Modifiers

    public void Refill(Entity<RefillableSolutionComponent?> entity, Entity<SolutionComponent> soln, Solution refill)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        AddSolution(soln, refill);
    }

    public void Inject(Entity<InjectableSolutionComponent?> entity, Entity<SolutionComponent> soln, Solution inject)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        AddSolution(soln, inject);
    }

    public Solution Drain(Entity<DrainableSolutionComponent?> entity, Entity<SolutionComponent> soln, FixedPoint2 quantity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return new();

        return SplitSolution(soln, quantity);
    }

    public Solution Draw(Entity<DrawableSolutionComponent?> entity, Entity<SolutionComponent> soln, FixedPoint2 quantity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return new();

        return SplitSolution(soln, quantity);
    }

    #endregion Solution Modifiers

    public float PercentFull(EntityUid uid)
    {
        if (!TryGetDrainableSolution(uid, out _, out var solution) || solution.MaxVolume.Equals(FixedPoint2.Zero))
            return 0;

        return solution.FillFraction * 100;
    }

    #region Static Methods

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

    #endregion Static Methods
}
