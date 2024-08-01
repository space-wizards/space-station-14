using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    private static SolutionComponent.ReagentData _invalidReagentData = new();
    private static SolutionComponent.VariantData _invalidVariantData = new();

    /// <summary>
    /// Optimize memory usage of reagent lists
    /// </summary>
    /// <param name="solution"></param>
    protected void TrimAllocs(Entity<SolutionComponent> solution)
    {
        //This is for memory optimization, there is no point to dirtying this since we aren't in a rush to sync it.
        solution.Comp.Contents.TrimExcess();
        solution.Comp.Contents.EnsureCapacity(ReagentAlloc);
        foreach (ref var reagentData in solution.Comp.ContentsSpan)
        {
            TrimAlloc(ref reagentData);
        }
    }

    /// <summary>
    /// Optimize variant memory usage for a given reagent
    /// </summary>
    /// <param name="reagentData"></param>
    protected void TrimAlloc(ref SolutionComponent.ReagentData reagentData)
    {
        if (reagentData.Variants?.Count == 0)
        {
            reagentData.Variants = null;
            return;
        }
        reagentData.Variants?.TrimExcess();
        reagentData.Variants?.EnsureCapacity(VariantAlloc);
    }

    /// <summary>
    /// Updates the primary reagent
    /// </summary>
    /// <param name="solution"></param>
    protected void UpdatePrimaryReagent(Entity<SolutionComponent> solution)
    {
        if (CheckIfEmpty(solution))
            return;
        var contents = solution.Comp.ContentsSpan;
        UpdatePrimaryReagent(solution, contents, GetPrimaryReagentQuantity(solution, contents));
    }

    /// <summary>
    /// Updates the primary reagent
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagentData"></param>
    /// <param name="delta"></param>
    protected void UpdatePrimaryReagent(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta)
    {
        if (!reagentData.IsValid || CheckIfEmpty(solution))
            return;
        var contentsSpan = solution.Comp.ContentsSpan;
        var primaryQuantity = GetPrimaryReagentQuantity(solution, contentsSpan);
        if (solution.Comp.PrimaryReagentIndex == reagentData.Index
            && FixedPoint2.Sign(delta) >= 0)
            return;
        if (reagentData.TotalQuantity >= primaryQuantity)
        {
            solution.Comp.PrimaryReagentIndex = reagentData.Index;
        }
        UpdatePrimaryReagent(solution, contentsSpan, primaryQuantity);
    }

    /// <summary>
    /// Updates the primary reagent
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="contents"></param>
    /// <param name="primaryReagentQuantity"></param>

    private void UpdatePrimaryReagent(
        Entity<SolutionComponent> solution,
        Span<SolutionComponent.ReagentData> contents,
        FixedPoint2 primaryReagentQuantity)
    {
        var newIndex = 0;
        foreach (ref var reagentData in contents)
        {
            if (reagentData.TotalQuantity > primaryReagentQuantity)
                newIndex = reagentData.Index;
        }

        if (newIndex == solution.Comp.PrimaryReagentIndex)
            return;
        solution.Comp.PrimaryReagentIndex = newIndex;
        UpdateAppearance((solution, solution, null));
    }

    /// <summary>
    /// Check if the solution is empty and update primaryReagent
    /// </summary>
    /// <param name="solution"></param>
    /// <returns></returns>
    protected bool CheckIfEmpty(Entity<SolutionComponent> solution)
    {
        if (solution.Comp.Volume != 0)
            return false;
        if (solution.Comp.PrimaryReagentIndex < 0)
            return true;
        solution.Comp.PrimaryReagentIndex = -1;
        return true;
    }

    /// <summary>
    /// Shifts Contents Indices after a reagent was removed
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="startingIndex"></param>
    protected void ShiftContentIndices(Entity<SolutionComponent> solution,
        int startingIndex)
    {
        if (startingIndex >= solution.Comp.Contents.Count)
            return;
        var contents = solution.Comp.ContentsSpan;
        for (var i = startingIndex; i < contents.Length; i++)
        {
            ref var reagentData = ref contents[i];
            reagentData.Index--;
            if (reagentData.Variants == null)
                continue;
            foreach (ref var variantData in reagentData.VariantsSpan)
            {
                variantData.ParentIndex = i;
            }
        }
    }


}
