using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Types;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{

    private static ReagentQuantity InvalidQuantity  = new();
    private static ReagentVariantQuantity InvalidVariantQuantity  = new();

    protected static bool IsValidQuantity(ref ReagentQuantity reagentQuantity)
        => reagentQuantity.Equals(InvalidQuantity);

    protected static bool IsValidQuantity(ref ReagentVariantQuantity variantQuantity)
        => variantQuantity.Equals(InvalidVariantQuantity);

    protected int GetReagentIndex(Entity<SolutionComponent> solution, string reagentId)
    {
        if (TryGetReagentIndex(solution, reagentId, out var index))
            return index;
        throw new KeyNotFoundException(
            $"Reagent with id:{reagentId} could not be found in solution:{ToPrettyString(solution)}");
    }

    protected bool TryGetReagentIndex(Entity<SolutionComponent> solution, string reagentId, out int index)
    {
        index = 0;
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (reagentData.ReagentId == reagentId)
                return true;
            index++;
        }
        index = -1;
        return false;
    }

    protected void TrimAllocs(Entity<SolutionComponent> solution)
    {
        //This is for memory optimization, there is no point to dirtying this since we aren't in a rush to sync it.
        solution.Comp.Contents.TrimExcess();
        solution.Comp.Contents.EnsureCapacity(ReagentAlloc);
        solution.Comp.VariantContents.TrimExcess();
        solution.Comp.VariantContents.EnsureCapacity(VariantAlloc);
    }

    protected void AddQuantityData(Entity<SolutionComponent> solution, ReagentQuantity quantity)
    {
        solution.Comp.Contents.Add(quantity);
        if (solution.Comp.PrimaryReagentIndex < 0)
            solution.Comp.PrimaryReagentIndex = 0;
        Dirty(solution);
    }

    protected void AddQuantityData(Entity<SolutionComponent> solution, ReagentVariantQuantity quantity)
    {
        solution.Comp.VariantContents.Add(quantity);
        Dirty(solution);
    }

    protected void RemoveQuantity(Entity<SolutionComponent> solution, ReagentQuantity quantity)
    {
        solution.Comp.Contents.Remove(quantity);
        if (solution.Comp.Contents.Count == 0)
            solution.Comp.PrimaryReagentIndex = -1;
        Dirty(solution);
    }

    protected void RemoveQuantity(Entity<SolutionComponent> solution, ReagentVariantQuantity quantity)
    {
        solution.Comp.VariantContents.Remove(quantity);
        Dirty(solution);
    }


    /// <summary>
    /// Completely removes all reagents from the solution
    /// </summary>
    /// <param name="solution">Target solution</param>
    protected void PurgeAllReagents(Entity<SolutionComponent> solution)
    {
       solution.Comp.Contents.Clear();
       solution.Comp.VariantContents.Clear();
       SetTotalVolume(solution, 0);
       TrimAllocs(solution);
       Dirty(solution);
    }

    /// <summary>
    /// Completely removes a reagent or reagent variant. If no variant is specified, this will remove all variants of the reagent,
    /// otherwise only the specified variant will be removed if found
    /// </summary>
    /// <param name="solution">Target solution</param>
    /// <param name="reagent">Reagent Type</param>
    /// <param name="variant">Variant Type</param>
    /// <param name="reagentIndex">Index of removed reagent, -1 if invalid</param>
    /// <param name="variantIndex">Index of removed variant, -1 if invalid</param>
    /// <param name="quantity">Quantity of reagent Purged</param>
    /// <returns>If successful</returns>
    protected bool PurgeReagent(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant? variant,
        out int reagentIndex,
        out int variantIndex,
        out FixedPoint2 quantity)
    {
        quantity = 0;
        reagentIndex = 0;
        variantIndex = -1;
        List<int>? variantIndices = null;
        var contents = CollectionsMarshal.AsSpan(solution.Comp.Contents);
        var variantContents = CollectionsMarshal.AsSpan(solution.Comp.VariantContents);
        foreach (ref var reagentData in contents)
        {
            if (reagent.Comp.Id != reagentData.ReagentId)
            {
                reagentIndex++;
                continue;
            }
            if (reagentData.VariantIndices is { Count: > 0 })
                variantIndices = reagentData.VariantIndices;
            break;
        }

        if (reagentIndex >= solution.Comp.Contents.Count - 1)
        {
            reagentIndex = -1;
            return false;
        }

        if (variantIndices is { Count: > 0 })
        {
            if (variant != null)
            {
                ReagentVariantQuantity? varData = null;
                foreach (var varIndex in variantIndices)
                {
                    ref var variantData = ref variantContents[varIndex];
                    if (variantData.Variant == null || !variantData.Variant!.Equals(variant))
                        continue;
                    variantIndex = varIndex;
                    varData = variantData;
                    break;
                }
                if (varData == null)
                    return false;
                ref var reagentData = ref contents[reagentIndex];
                quantity += varData.Value.Quantity;
                reagentData.Quantity -= varData.Value.Quantity;
                ChangeTotalVolume(solution, varData.Value.Quantity);
                if (reagentData.Quantity <= 0 && (reagentData.VariantIndices == null || reagentData.VariantIndices.Count == 0))
                    solution.Comp.Contents.RemoveAt(reagentIndex);
                solution.Comp.VariantContents.RemoveAt(variantIndex);
                TrimAllocs(solution);
                Dirty(solution);
                return true;
            }
            else
            {
                var reagentData = solution.Comp.Contents[reagentIndex];
                quantity += reagentData.Quantity;
                ChangeTotalVolume(solution, reagentData.Quantity);
                solution.Comp.Contents.RemoveAt(reagentIndex);
                TrimAllocs(solution);
                Dirty(solution);
                return true;
            }
        }
        if (variant != null)
                return false;
        var reagentData2 = solution.Comp.Contents[reagentIndex];
        quantity += reagentData2.Quantity;
        ChangeTotalVolume(solution, reagentData2.Quantity);
        solution.Comp.Contents.RemoveAt(reagentIndex);
        TrimAllocs(solution);
        Dirty(solution);
        return true;
    }

}
