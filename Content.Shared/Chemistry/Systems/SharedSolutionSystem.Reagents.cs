using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Collections;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    #region Ensures/Getters

    /// <summary>
    /// Ensures that the specified reagent will be present in the solution
    /// </summary>
    /// <param name="solution">target solution</param>
    /// <param name="reagent">reagent to add</param>
    /// <param name="variant"></param>
    [PublicAPI]
    public void EnsureReagent(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant? variant = null)
    {
        if (variant == null)
        {
            EnsureReagentDataRef(solution, reagent);
            return;
        }
        EnsureVariantDataRef(solution, reagent, variant);
    }

    /// <summary>
    /// Attempt to get the quantity of a reagent
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="quantity"></param>
    /// <param name="variant"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetReagentQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        out FixedPoint2 quantity,
        ReagentVariant? variant = null)
    {
        quantity = 0;
        if (variant == null)
        {
            ref var reagentData = ref GetReagentDataRef(solution, reagent);
            if (!reagentData.IsValid)
                return false;
            quantity = reagentData.Quantity;
            return true;
        }
        ref var variantData = ref GetVariantDataRef(solution, reagent, variant);
        if (!variantData.IsValid)
            return false;
        quantity = variantData.Quantity;
        return true;
    }

    /// <summary>
    /// Attempt to get the total quantity of a reagent and it's variants
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="totalQuantity"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetTotalQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        out FixedPoint2 totalQuantity)
    {
        totalQuantity = 0;
        ref var reagentData = ref GetReagentDataRef(solution, reagent);
        if (!reagentData.IsValid)
            return false;
        totalQuantity = reagentData.TotalQuantity;
        return true;
    }

    /// <summary>
    /// Get the quantity of a reagent, returning -1 if a reagent is not found
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <returns></returns>
    [PublicAPI]
    public FixedPoint2 GetQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant? variant = null)
    {
        if (!TryGetReagentQuantity(solution, reagent, out var quantity, variant))
            return -1;
        return quantity;
    }

    /// <summary>
    /// Get the quantity of a reagent, returning -1 if a reagent is not found
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <returns></returns>
    [PublicAPI]
    public ReagentQuantity GetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant? variant = null)
    {
        return !TryGetReagentQuantity(solution, reagent, out var quantity, variant)
            ? new ReagentQuantity()
            : new ReagentQuantity(new ReagentDef(reagent, variant), quantity);
    }

    /// <summary>
    /// Enumerates all reagents
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="includeVariants"></param>
    /// <returns></returns>
    [PublicAPI]
    public IEnumerable<ReagentDef> EnumerateReagents(Entity<SolutionComponent> solution,
        bool includeVariants = false
        )
    {
        foreach (var reagentData in solution.Comp.Contents)
        {
            yield return reagentData;
            if (!includeVariants || reagentData.Variants == null)
                continue;
            foreach (var variantData in reagentData.Variants)
            {
                yield return new(reagentData.ReagentEnt, variantData.Variant);
            }
        }
    }

    /// <summary>
    /// Enumerates all ReagentVariants, but not their base reagents
    /// </summary>
    /// <param name="solution"></param>
    /// /// <returns></returns>
    [PublicAPI]
    public IEnumerable<ReagentDef> EnumerateReagentVariants(Entity<SolutionComponent> solution)
    {
        return EnumerateReagentVariantsOfType<ReagentVariant>(solution, true);
    }

    /// <summary>
    /// Enumerates only ReagentVariants of the specified type
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="includeChildTypes"></param>
    /// <returns></returns>
    [PublicAPI]
    public IEnumerable<ReagentDef> EnumerateReagentVariantsOfType<T>(Entity<SolutionComponent> solution,
        bool includeChildTypes = false) where T: ReagentVariant
    {
        foreach (var reagentData in solution.Comp.Contents)
        {
            if (reagentData.Variants == null)
                continue;
            foreach (var variantData in reagentData.Variants)
            {
                if (includeChildTypes)
                {
                    if (variantData.Variant.GetType().IsAssignableFrom(typeof(T)))
                        continue;
                }
                else
                {
                    if (variantData.Variant.GetType() == typeof(T))
                        continue;
                }
                yield return new ReagentDef(reagentData.ReagentEnt, variantData.Variant);
            }
        }
    }

    /// <summary>
    /// Gets the current primary reagent if there is one
    /// </summary>
    /// <param name="solution"></param>
    /// <returns></returns>
    public ReagentQuantity? GetPrimaryReagent(Entity<SolutionComponent> solution)
    {
        return solution.Comp.PrimaryReagentIndex < 0
               || solution.Comp.PrimaryReagentIndex >= solution.Comp.Contents.Count
            ? null
            : solution.Comp.Contents[solution.Comp.PrimaryReagentIndex];
    }

    /// <summary>
    /// Tries to get the current primary reagent if there is one
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <returns></returns>
    public bool TryGetPrimaryReagent(Entity<SolutionComponent> solution,
        out ReagentQuantity reagent)
    {
        var foundReagent = GetPrimaryReagent(solution);
        if (foundReagent == null)
        {
            reagent = new ReagentQuantity();
            return false;
        }
        reagent = foundReagent.Value;
        return true;
    }

    #endregion

    #region QuantityOperations

    /// <summary>
    /// Sets the quantity of a reagent or reagent variant. This respects max volume if force is set to true
    /// </summary>
    /// <param name="solution">Target solution</param>
    /// <param name="reagent">Target Reagent</param>
    /// <param name="newQuantity">New Quantity value</param>
    /// <param name="overflow">How much </param>
    /// <param name="variant">Reagent Variant Data</param>
    /// <param name="force">Should this ignore maxVolume</param>
    [PublicAPI]
    public void SetReagent(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 newQuantity,
        out FixedPoint2 overflow,
        ReagentVariant? variant = null,
        bool force = true)
    {
        overflow = 0;
        ref var reagentData = ref EnsureReagentDataRef(solution, reagent);
        if (variant == null)
        {
                ChangeReagentQuantity_Implementation(solution,
                    ref reagentData,
                    newQuantity - reagentData.Quantity,
                    out overflow,
                    force);
            return;
        }
        ref var variantData = ref EnsureVariantDataRef(solution, reagent, variant);
        ChangeVariantQuantity_Implementation(solution,
            ref reagentData,
            ref variantData,
            newQuantity - variantData.Quantity,
            out overflow,
            force);
    }

    /// <summary>
    /// Change the quantity of a reagent or reagent variant by the value specified
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="quantity"></param>
    /// <param name="overflow"></param>
    /// <param name="variant"></param>
    /// <param name="force"></param>
    /// <returns>True if successful</returns>
    [PublicAPI]
    public bool AddReagent(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        ReagentVariant? variant = null,
        bool force = true)
    {
        overflow = 0;
        if (quantity == 0)
            return true;
        ref var reagentData = ref GetReagentDataRef(solution, reagent);
        if (!reagentData.IsValid)
            return false;
        if (variant == null)
        {
            ChangeReagentQuantity_Implementation(solution,
                ref reagentData,
                quantity,
                out overflow,
                force);
            return true;
        }
        ref var variantData = ref GetVariantDataRef(solution, reagent, variant);
        if (!variantData.IsValid)
            return false;
        ChangeVariantQuantity_Implementation(solution,
            ref reagentData,
            ref variantData,
            quantity,
            out overflow,
            force);
        return true;
    }

    [PublicAPI]
    public bool SetReagentTotalQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        bool force = true)
    {
        overflow = 0;
        if (quantity == 0)
            return true;
        ref var reagentData = ref GetReagentDataRef(solution, reagent);
        if (!reagentData.IsValid)
            return false;
        ChangeReagentTotalQuantity_Implementation(solution,
            ref reagentData,
            quantity - reagentData.TotalQuantity,
            out overflow,
            force);
        return true;
    }

    [PublicAPI]
    public bool AddReagentTotalQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        bool force = true)
    {
        overflow = 0;
        if (quantity == 0)
            return true;
        ref var reagentData = ref GetReagentDataRef(solution, reagent);
        if (!reagentData.IsValid)
            return false;
        ChangeReagentTotalQuantity_Implementation(solution, ref reagentData, quantity, out overflow, force);
        return true;
    }

    [PublicAPI]
    public void SplitSolution(Entity<SolutionComponent> originSolution,
        Entity<SolutionComponent>? targetSolution,
        out FixedPoint2 overflow,
        bool force = true)
    {
        if (targetSolution == null)
        {
            ScaleSolution(originSolution, 0.5f, out overflow, true, force);
            return;
        }
        TransferSolution(originSolution,
            targetSolution.Value,
            originSolution.Comp.Volume * 0.5,
            out overflow,
            force);
    }


    [PublicAPI]
    public void ScaleSolution(Entity<SolutionComponent> solution,
        float scalingFactor,
        out FixedPoint2 overflow,
        bool byCurrentVolume = true,
        bool force = true)
    {
        scalingFactor = MathF.Abs(scalingFactor);
        var volume = solution.Comp.MaxVolume;
        if (byCurrentVolume)
            volume = solution.Comp.Volume;
        var delta = volume * scalingFactor - volume;
        ChangeSolutionVolume(solution, delta, out overflow, force);
    }

    [PublicAPI]
    public void ChangeSolutionVolume(Entity<SolutionComponent> solution,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        bool force = true)
    {
        overflow = 0;
        WillOverflow(solution, quantity, out overflow);
        if ( !force)
            quantity -= overflow;
        if (quantity == 0)
            return;
        overflow = -ChangeAllDataQuantities(solution, quantity);
    }

    [PublicAPI]
    public void TransferSolution(Entity<SolutionComponent> originSolution,
        Entity<SolutionComponent> targetSolution,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        bool force = true)
    {
        overflow = 0;
        if (quantity == 0)
            return;
        if (quantity < 0)
        {
            quantity = -quantity;
            (originSolution, targetSolution) = (targetSolution, originSolution);
        }
        WillOverflow(targetSolution, quantity, out overflow);
        if (!force)
        {
            quantity -= overflow;
        }
        if (quantity == 0)
            return;
        var missing = ChangeAllDataQuantities(originSolution, -quantity);
        if (missing >= quantity)
            return;
        overflow -= missing;
        //we don't care about underflow here because it isn't possible
        ChangeAllDataQuantities(targetSolution, quantity - missing);
    }

    #endregion

    #region Internal

    /// <summary>
    /// Ensures that reagentData is present for the specified reagent in the solution
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <returns>Reference to reagentData</returns>
    protected ref SolutionComponent.ReagentData EnsureReagentDataRef(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent)
    {
        foreach (ref var quantityData in solution.Comp.ContentsSpan)
        {
            if (quantityData.ReagentId == reagent.Comp.Id)
                return ref quantityData;
        }
        var index = solution.Comp.Contents.Count - 1;
        solution.Comp.Contents.Add(new SolutionComponent.ReagentData(reagent, 0, index));
        Dirty(solution);
        return ref solution.Comp.ContentsSpan[index];
    }

    /// <summary>
    /// Ensures that variantData is present for the specified reagent in the solution
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <returns>Reference to variantData</returns>
    protected ref SolutionComponent.VariantData EnsureVariantDataRef(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant)
    {
        ref var quantData = ref EnsureReagentDataRef(solution, reagent);
        foreach (ref var varData in quantData.VariantsSpan)
        {
            if (varData.Variant.Equals(variant))
                return ref varData;
        }
        quantData.Variants ??= new(VariantAlloc);
        quantData.Variants.Add(new SolutionComponent.VariantData(variant, 0, quantData.Index));
        solution.Comp.ReagentVariantCount++;
        Dirty(solution);
        return ref quantData.VariantsSpan[^1];
    }

    /// <summary>
    /// Gets reagentData if present, or returns an invalid one
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <returns>Found reagentData or invalid one</returns>
    protected ref SolutionComponent.ReagentData GetReagentDataRef(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent)
    {
        foreach (ref var reagentData in solution.Comp.ContentsSpan)
        {
            if (reagentData.ReagentId == reagent.Comp.Id)
                return ref reagentData;
        }
        return ref _invalidReagentData;
    }

    /// <summary>
    /// Gets variantData if present, or returns an invalid one
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <returns>Found variantData or invalid one</returns>
    protected ref SolutionComponent.VariantData GetVariantDataRef(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant)
    {
        ref var reagentData = ref GetReagentDataRef(solution, reagent);
        if (!reagentData.IsValid)
            return ref _invalidVariantData;
        foreach (ref var variantData in reagentData.VariantsSpan)
        {
            if (variantData.Variant.Equals(variant))
                return ref variantData;
        }
        return ref _invalidVariantData;
    }

    /// <summary>
    /// Changes the quantity in the specified reagentData
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagentData"></param>
    /// <param name="delta"></param>
    /// <returns>Underflow ammount</returns>
    protected FixedPoint2 ChangeReagentDataQuantity(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta)
    {
        var underflow = FixUnderflow(reagentData.Quantity, ref delta);
        if (delta == 0)
            return underflow;
        reagentData.Quantity += delta;
        reagentData.TotalQuantity += delta;
        Dirty(solution);
        ChangeTotalVolume(solution,ref reagentData, delta);
        return underflow;
    }

    /// <summary>
    /// Changes the quantity in the specified variantData
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="parentData"></param>
    /// /// <param name="variantData"></param>
    /// <param name="delta"></param>
    /// <returns>Underflow</returns>
    protected FixedPoint2 ChangeVariantDataQuantity(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData parentData,
        ref SolutionComponent.VariantData variantData,
        FixedPoint2 delta)
    {
        var underflow = FixUnderflow(variantData.Quantity, ref delta);
        if (delta == 0)
            return underflow;
        variantData.Quantity += delta;
        parentData.TotalQuantity += delta;
        Dirty(solution);
        ChangeTotalVolume(solution,ref parentData, delta);
        return underflow;
    }

    protected FixedPoint2 ChangeAllReagentVariantDataQuantity(
        Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta)
    {
        //If there are no variants, then just use the regular reagent method
        if (reagentData.Variants == null || reagentData.TotalQuantity == reagentData.Quantity)
            return ChangeReagentDataQuantity(solution, ref reagentData, delta);

        //Start with the base reagent
        delta += ChangeReagentDataQuantity(solution,
            ref reagentData,
            ClampToEpsilon(delta.Float() / reagentData.VariantCount+1));

        //shuffle the order we check variants so that they aren't always accessed in the
        //order of how old they are.
        var validCount = reagentData.VariantCount;
        var shuffle = CreateRandomIndexer(validCount);

        var skip = new bool[validCount];
        Array.Fill(skip, false);

        var variants = reagentData.VariantsSpan;
        for (var i = 0; i < variants.Length; i++)
        {
            if (validCount == 0 || delta == 0)
                break;
            if (skip[i])
                continue;
            var variantData = variants[shuffle[i]];
            if (variantData.Quantity == 0)
            {
                skip[i] = true;
                validCount--;
                continue;
            }

            var localDelta = ClampToEpsilon(delta.Float() / validCount);
            var underflow = ChangeVariantDataQuantity(solution, ref reagentData, ref variantData, localDelta);
            if (underflow > 0) //If we have an underflow update re-add it back to the delta and decrement the valid count.
            {
                skip[i] = true;
                validCount--;
                delta += underflow;
            }
            //if we hit the end keep looping until we no longer have any delta left.
            if (i == variants.Length-1 && delta > 0)
                i = 0;
        }
        return ChangeReagentDataQuantity(solution, ref reagentData, delta);
    }

    protected FixedPoint2 ChangeAllDataQuantities(
        Entity<SolutionComponent> solution,
        FixedPoint2 delta)
    {
        //create an indexShuffler so that reagents aren't accessed only by oldest
        var validCount = solution.Comp.Contents.Count;
        var shuffle = CreateRandomIndexer(solution.Comp.Contents.Count);
        var contents = solution.Comp.ContentsSpan;

        var skip = new bool[validCount];
        Array.Fill(skip, false);

        for (var i = 0; i < contents.Length; i++)
        {
            if (validCount == 0 || delta == 0)
                break;
            if (skip[i])
                continue;
            var reagentData = contents[shuffle[i]];
            if (reagentData.Quantity == 0)
            {
                validCount--;
                skip[i] = true;
                continue;
            }
            var localDelta = ClampToEpsilon(delta.Float() / validCount);
            var underflow = ChangeAllReagentVariantDataQuantity(solution, ref reagentData, localDelta);
            if (underflow > 0) //If we have an underflow update re-add it back to the delta and decrement the valid count.
            {
                validCount--;
                skip[i] = true;
                delta += underflow;
            }
            //if we hit the end keep looping until we no longer have any delta left.
            if (i == contents.Length-1)
                i = 0;
        }
        return delta;
    }

    protected void ChangeReagentQuantity_Implementation(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        bool force = true)
    {
        overflow = 0;
        if (!force && WillOverflow(solution, delta, out overflow))
            delta -= overflow;
        overflow -= ChangeReagentDataQuantity(solution, ref reagentData , delta);
    }

    protected void ChangeVariantQuantity_Implementation(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        ref SolutionComponent.VariantData variantData,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        bool force = true)
    {
        overflow = 0;
        if (!force && WillOverflow(solution, delta, out overflow))
            delta -= overflow;
        overflow -= ChangeVariantDataQuantity(solution, ref reagentData, ref variantData , delta);
    }

    protected void ChangeReagentTotalQuantity_Implementation(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        bool force)
    {
        overflow = 0;
        WillOverflow(solution, delta, out overflow);
        if (!force)
            delta -= overflow;
        overflow = -ChangeAllReagentVariantDataQuantity(solution, ref reagentData, delta);
    }


    /// <summary>
    /// Gets the quantity of the primary reagent or 0 if there are none
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="contents"></param>
    /// <returns></returns>
    protected FixedPoint2 GetPrimaryReagentQuantity(Entity<SolutionComponent> solution,
        Span<SolutionComponent.ReagentData> contents)
    {
        return solution.Comp.Volume == 0
               || solution.Comp.PrimaryReagentIndex < 0
               || solution.Comp.PrimaryReagentIndex >= solution.Comp.Contents.Count
            ? 0
            : contents[solution.Comp.PrimaryReagentIndex].TotalQuantity;
    }

    /// <summary>
    /// Purges a reagent from the solution
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagentData"></param>
    /// <returns></returns>
    protected bool PurgeReagent(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData)
    {
        if (!reagentData.IsValid)
            return false;
        var index = reagentData.Index;
        ChangeTotalVolume(solution, ref reagentData, -reagentData.TotalQuantity);
        solution.Comp.ReagentVariantCount -= reagentData.VariantCount;
        solution.Comp.Contents.RemoveAt(index);
        ShiftContentIndices(solution, index);
        UpdatePrimaryReagent(solution);
        return true;
    }

    /// <summary>
    /// Purges a reagentVariant from the solution
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="variantData"></param>
    /// <param name="removeBaseIfLast"></param>
    /// <returns></returns>
    protected bool PurgeReagent(
        Entity<SolutionComponent> solution,
        ref SolutionComponent.VariantData variantData,
        bool removeBaseIfLast = false)
    {
        if (!variantData.IsValid)
            return false;
        ref var reagentData = ref solution.Comp.ContentsSpan[variantData.ParentIndex];
        if (reagentData.Variants == null)
            return false;

        var i = 0;
        foreach (ref var variant in reagentData.VariantsSpan)
        {
            if (variant.Variant.Equals(variantData.Variant))
                break;
            i++;
        }

        if (i >= reagentData.Variants.Count)
            return false;

        reagentData.TotalQuantity -= variantData.Quantity;
        ChangeTotalVolume(solution, ref reagentData, variantData.Quantity);
        reagentData.Variants?.RemoveAt(i);
        solution.Comp.ReagentVariantCount--;
        if (removeBaseIfLast && reagentData.VariantCount == 0)
        {
            return PurgeReagent(solution, ref reagentData);
        }
        UpdatePrimaryReagent(solution);
        return true;
    }

    /// <summary>
    /// Completely removes all reagents from the solution
    /// </summary>
    /// <param name="solution">Target solution</param>
    protected void PurgeAllReagents(Entity<SolutionComponent> solution)
    {
       solution.Comp.Contents.Clear();
       SetTotalVolume(solution, 0);
       TrimAllocs(solution);
    }
    #endregion

}
