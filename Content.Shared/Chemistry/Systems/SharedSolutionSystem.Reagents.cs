using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    #region Ensures/Getters

    /// <summary>
    /// Ensures that the specified reagent will be present in the solution
    /// </summary>
    /// <param name="solution">target solution</param>
    /// <param name="reagent">reagent to add</param>
    [PublicAPI]
    public void EnsureReagent(Entity<SolutionComponent> solution,
        ReagentDef reagent)
    {
        if (reagent.Variant == null)
        {
            EnsureReagentData(solution, reagent);
            return;
        }
        EnsureVariantData(solution, reagent, reagent.Variant);
    }

    /// <summary>
    /// Attempt to get the quantity of a reagent
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="quantity"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetReagentQuantity(
        Entity<SolutionComponent> solution,
        ReagentDef reagent,
        out FixedPoint2 quantity)
    {
        quantity = 0;
        if (!reagent.IsValid || !TryGetReagentDataIndex(solution, reagent, out var index))
            return false;
        ref var reagentData = ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[index];
        if (reagent.Variant == null)
        {
            quantity = reagentData.Quantity;
            return true;
        }
        if (!TryGetVariantDataIndex(solution, reagent, reagent.Variant, out var varIndex))
            return false;
        quantity = CollectionsMarshal.AsSpan(reagentData.Variants)[varIndex].Quantity;
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
        ReagentDef reagent,
        out FixedPoint2 totalQuantity)
    {
        totalQuantity = 0;
        if (!reagent.IsValid || !TryGetReagentDataIndex(solution, reagent, out var index))
            return false;
        totalQuantity = CollectionsMarshal.AsSpan(solution.Comp.Contents)[index].TotalQuantity;
        return true;
    }

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        params ReagentDef[] reagents)
    {
        FixedPoint2 totalQuantity = 0;
        foreach (var reagentDef in reagents)
        {
            totalQuantity += GetTotalQuantity(solution, reagentDef);
        }
        return totalQuantity;
    }

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        ReagentDef reagent)
    {
        FixedPoint2 totalQuantity = 0;
        if (!reagent.IsValid ||!TryGetReagentDataIndex(solution, reagent, out var index))
            return totalQuantity;
        totalQuantity = CollectionsMarshal.AsSpan(solution.Comp.Contents)[index].TotalQuantity;
        return totalQuantity;
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
        ReagentDef reagent,
        ReagentVariant? variant = null)
    {
        if (!TryGetReagentQuantity(solution, reagent, out var quantity))
            return -1;
        return quantity;
    }

    /// <summary>
    /// Get the quantity of a reagent, returning -1 if a reagent is not found
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <returns></returns>
    [PublicAPI]
    public ReagentQuantity GetReagentQuantity(Entity<SolutionComponent> solution,
        ReagentDef reagent)
    {
        return !TryGetReagentQuantity(solution, reagent, out var quantity)
            ? new ReagentQuantity()
            : new ReagentQuantity(reagent, quantity);
    }

    /// <summary>
    /// Enumerates all reagents
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="includeVariants"></param>
    /// <returns></returns>
    [PublicAPI]
    public IEnumerable<ReagentQuantity> EnumerateReagents(Entity<SolutionComponent> solution,
        bool includeVariants = false
        )
    {
        foreach (var reagentData in solution.Comp.Contents)
        {
            yield return new ReagentQuantity(reagentData, reagentData.Quantity);
            if (!includeVariants || reagentData.Variants == null)
                continue;
            foreach (var variantData in reagentData.Variants)
            {
                yield return new (new ReagentDef(reagentData.ReagentEnt, variantData.Variant), variantData.Quantity);
            }
        }
    }

    /// <summary>
    /// Enumerates all ReagentVariants, but not their base reagents
    /// </summary>
    /// <param name="solution"></param>
    /// /// <returns></returns>
    [PublicAPI]
    public IEnumerable<ReagentQuantity> EnumerateReagentVariants(Entity<SolutionComponent> solution)
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
    public IEnumerable<ReagentQuantity> EnumerateReagentVariantsOfType<T>(Entity<SolutionComponent> solution,
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
                yield return new (new ReagentDef(reagentData.ReagentEnt, variantData.Variant), variantData.Quantity);
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

    public FixedPoint2 GetContainerReagentQuantity(Entity<SolutionHolderComponent?> solutionContainer, ReagentDef reagent)
    {
        if (!Resolve(solutionContainer, ref solutionContainer.Comp))
            return 0;

        var reagentQuantity = FixedPoint2.New(0);
        if (!TryComp(solutionContainer, out SolutionHolderComponent? solConComp))
            return FixedPoint2.Zero;
        foreach (var solutionEnt in EnumerateSolutions((solutionContainer, solConComp)))
        {
            reagentQuantity += GetTotalQuantity(solutionEnt, reagent);
        }
        return reagentQuantity;
    }


    #endregion

    #region QuantityOperations

    /// <summary>
    /// Sets the quantity of a reagent or reagent variant. This respects max volume if force is set to true
    /// </summary>
    /// <param name="solution">Target solution</param>
    /// <param name="reagentQuantity">New Quantity value</param>
    /// <param name="overflow">How much </param>
    /// <param name="temperature"></param>
    /// <param name="force">Should this ignore maxVolume</param>
    [PublicAPI]
    public void SetReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantity reagentQuantity,
        out FixedPoint2 overflow,
        float? temperature = null,
        bool force = true)
    {
        overflow = 0;
        if (!reagentQuantity.IsValid)
            return;
        ref var reagentData = ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[EnsureReagentData(solution, reagentQuantity)];
        if (reagentQuantity.ReagentDef.Variant == null)
        {
                ChangeReagentQuantity_Implementation(solution,
                    ref reagentData,
                    reagentQuantity.Quantity - reagentData.Quantity,
                    out overflow,
                    temperature,
                    force);
            return;
        }
        ref var variantData = ref CollectionsMarshal.AsSpan(reagentData.Variants)[EnsureVariantData(solution,
            reagentQuantity.ReagentDef.Entity,
            reagentQuantity.ReagentDef.Variant)];
        ChangeVariantQuantity_Implementation(solution,
            ref reagentData,
            ref variantData,
            reagentQuantity.Quantity - variantData.Quantity,
            out overflow,
            temperature,
            force);
    }


    [PublicAPI]
    public bool RemoveReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantity reagentQuantity,
        out FixedPoint2 underFlow,
        float? temperature = null,
        bool force = true,
        bool purge = false)
    {
        underFlow = 0;
        if (!reagentQuantity.IsValid)
            return false;
        if (reagentQuantity.Quantity == 0 && !purge)
        {
            return true;
        }

        if (!TryGetReagentDataIndex(solution, reagentQuantity.ReagentDef, out var reagentIndex))
            return false;
        ref var reagentData = ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[reagentIndex];
        if (reagentQuantity.Variant == null)
        {
            if (reagentQuantity.Quantity > 0)
            {
                ChangeReagentQuantity_Implementation(solution,
                ref reagentData,
                -reagentQuantity.Quantity,
                out underFlow,
                temperature,
                force);
            }

            if (!purge)
                return true;
            PurgeReagent(solution, ref reagentData);
            return true;
        }
        if (!TryGetVariantDataIndex(solution, reagentQuantity, reagentQuantity.Variant, out var index))
            return false;
        ref var varData = ref CollectionsMarshal.AsSpan(reagentData.Variants)[index];
        ChangeVariantQuantity_Implementation(solution,
            ref reagentData,
            ref varData,
            -reagentQuantity.Quantity,
            out underFlow,
            temperature,
            force);
        if (purge)
            PurgeReagent(solution, ref varData);
        return true;
    }

    /// <summary>
    /// Change the quantity of a reagent or reagent variant by the value specified
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagentQuantity"></param>
    /// <param name="overflow"></param>
    /// <param name="temperature"></param>
    /// <param name="force"></param>
    /// <returns>True if successful</returns>
    [PublicAPI]
    public bool AddReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantity reagentQuantity,
        out FixedPoint2 overflow,
        float? temperature = null,
        bool force = true)
    {
        overflow = 0;
        if (!reagentQuantity.IsValid)
            return false;
        if (reagentQuantity.Quantity == 0)
            return true;
        if (!TryGetReagentDataIndex(solution, reagentQuantity, out var reagentIndex))
            return false;
        ref var reagentData = ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[reagentIndex];
        if (reagentQuantity.Variant == null)
        {
            ChangeReagentQuantity_Implementation(solution,
                ref reagentData,
                reagentQuantity,
                out overflow,
                temperature,
                force);
            return true;
        }
        if (!TryGetVariantDataIndex(solution, reagentQuantity, reagentQuantity.Variant, out var index))
            return false;
        ChangeVariantQuantity_Implementation(solution,
            ref reagentData,
            ref CollectionsMarshal.AsSpan(reagentData.Variants)[index],
            reagentQuantity,
            out overflow,
            temperature,
            force);
        return true;
    }

    [PublicAPI]
    public bool SetReagentTotalQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        float? temperature = null,
        bool force = true)
    {
        overflow = 0;
        if (quantity == 0)
            return true;
        if (!TryGetReagentDataIndex(solution, reagent, out var reagentIndex))
            return false;
        ref var reagentData = ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[reagentIndex];
        ChangeReagentTotalQuantity_Implementation(solution,
            ref reagentData,
            quantity - reagentData.TotalQuantity,
            out overflow,
            temperature,
            force);
        return true;
    }

    [PublicAPI]
    public bool AddReagentTotalQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        float? temperature = null,
        bool force = true)
    {
        overflow = 0;
        if (quantity == 0)
            return true;
        if (!TryGetReagentDataIndex(solution, reagent, out var reagentIndex))
            return false;
        ChangeReagentTotalQuantity_Implementation(solution,
            ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[reagentIndex],
            quantity,
            out overflow,
            temperature,
            force);
        return true;
    }

    [PublicAPI]
    public FixedPoint2 RemoveReagents(Entity<SolutionComponent> solution,
        FixedPoint2 quantity)
    {
        var scaleFactor = Math.Clamp(1f-quantity.Float() / solution.Comp.Volume.Float(), 0f, 1.0f);
        if (scaleFactor == 0)
            return 0;
        var removedAmount = scaleFactor * solution.Comp.Volume;
        ScaleSolution(solution, scaleFactor, out var overflow);
        return removedAmount + overflow;
    }

    [PublicAPI]
    public FixedPoint2 RemoveReagents(Entity<SolutionComponent> solution,
        params ReagentQuantity[] reagents)
    {
        if (reagents.Length == 0)
            return 0;
        var contentsSpan = CollectionsMarshal.AsSpan(solution.Comp.Contents);
        FixedPoint2 overflow = 0;
        foreach (ref var reagentQuant in reagents.AsSpan())
        {
            if (!reagentQuant.IsValid
                || !TryGetReagentDataIndex(solution, reagentQuant.ReagentDef.Entity, out var index))
                continue;
            ref var reagentData = ref contentsSpan[index];
            if (reagentQuant.ReagentDef.Variant != null)
            {
                if (!TryGetVariantDataIndex(solution,
                        reagentQuant.ReagentDef.Entity,
                        reagentQuant.ReagentDef.Variant,
                        out var varIndex))
                    continue;
                ref var variantData = ref CollectionsMarshal.AsSpan(reagentData.Variants)[varIndex];
                ChangeVariantQuantity_Implementation(solution,
                    ref reagentData,
                    ref variantData,
                    -reagentQuant.Quantity,
                    out var localOverflow,
                    null,
                    true);
                overflow += localOverflow;
                continue;
            }
            ChangeReagentQuantity_Implementation(solution,
                ref reagentData,
                -reagentQuant.Quantity,
                out var localOverflow2,
                null,
                true);
            overflow += localOverflow2;
        }
        return overflow;
    }

    [PublicAPI]
    public FixedPoint2 AddReagents(Entity<SolutionComponent> solution,
        float? temperature = null,
        bool force = true,
        params ReagentQuantity[] reagents)
    {
        var contentsSpan = CollectionsMarshal.AsSpan(solution.Comp.Contents);
        FixedPoint2 overflow = 0;
        foreach (ref var reagentQuant in reagents.AsSpan())
        {
            if (!reagentQuant.IsValid)
                continue;
            ref var reagentData = ref contentsSpan[EnsureReagentData(solution, reagentQuant.ReagentDef.Entity)];
            if (reagentQuant.ReagentDef.Variant != null)
            {
                ref var variantData = ref CollectionsMarshal.AsSpan(reagentData.Variants)[EnsureVariantData(solution,
                    reagentQuant.ReagentDef.Entity,
                    reagentQuant.ReagentDef.Variant)];
                ChangeVariantQuantity_Implementation(solution,
                    ref reagentData,
                    ref variantData,
                    reagentQuant.Quantity,
                    out var localOverflow,
                    temperature ,
                    force);
                overflow += localOverflow;
                continue;
            }
            ChangeReagentQuantity_Implementation(solution,
                ref reagentData,
                reagentQuant.Quantity,
                out var localOverflow2,
                temperature,
                force);
            overflow += localOverflow2;
        }
        return overflow;
    }

    [PublicAPI]
    public SolutionContents SplitSolution(Entity<SolutionComponent> originSolution, float percentage = 0.5f)
    {
        ScaleSolution(originSolution, percentage, out var overflow, true, true);
        return GetReagents(originSolution,1f-percentage);
    }

    [PublicAPI]
    public SolutionContents SplitSolution(Entity<SolutionComponent> solution, FixedPoint2 quantity)
    {
        var percentage = quantity.Float()/solution.Comp.Volume.Float();
        var contents = GetReagents(solution, percentage);
        var overFlow = RemoveReagents(solution,contents);
        contents.Scale(100 - overFlow.Float() / contents.Volume.Float());
        return contents;
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
        overflow = -ChangeAllDataQuantities(solution, quantity, null);
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
        var missing = ChangeAllDataQuantities(originSolution, -quantity, null);
        if (missing >= quantity)
            return;
        overflow -= missing;
        //we don't care about underflow here because it isn't possible
        ChangeAllDataQuantities(targetSolution, quantity - missing, originSolution.Comp.Temperature);
    }

    public void RemoveAllReagents(Entity<SolutionComponent> solution,  bool keepTemperature = true, bool purge = false)
    {
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            reagentData.Quantity = 0;
            reagentData.TotalQuantity = 0;
            if (reagentData.Variants == null)
                continue;
            foreach (ref var variantData in CollectionsMarshal.AsSpan(reagentData.Variants))
            {
                variantData.Quantity = 0;
            }
        }
        solution.Comp.Volume = 0;
        Dirty(solution);
        ClearHeatCapacity(solution);
        ClearThermalEnergy(solution, !keepTemperature);
        if (purge)
            PurgeAllReagents(solution);
    }

    #endregion

    #region Internal

    protected SolutionContents GetReagents(Entity<SolutionComponent> solution, float percentage = 1.0f)
    {
        var contents = new ReagentQuantity[solution.Comp.ReagentAndVariantCount];
        var i = 0;
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            contents[i] = new(reagentData.ReagentEnt, reagentData.Quantity*percentage);
            if (reagentData.Variants == null)
            {
                i++;
                continue;
            }
            foreach (ref var variantData in CollectionsMarshal.AsSpan(reagentData.Variants))
            {
                contents[i] = new(reagentData.ReagentEnt, variantData.Quantity * percentage, variantData.Variant);
                i++;
            }
        }
        return new (solution.Comp.Temperature, contents);
    }


    /// <summary>
    /// Changes the quantity in the specified reagentData
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagentData"></param>
    /// <param name="delta"></param>
    /// <param name="temperature"></param>
    /// <returns>Underflow ammount</returns>
    protected FixedPoint2 ChangeReagentDataQuantity(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta,
        float? temperature)
    {
        var underflow = FixUnderflow(reagentData.Quantity, ref delta);
        if (delta == 0)
            return underflow;
        reagentData.Quantity += delta;
        reagentData.TotalQuantity += delta;
        ChangeTotalVolume(solution,ref reagentData, delta, temperature);
        Dirty(solution);
        return underflow;
    }

    /// <summary>
    /// Changes the quantity in the specified variantData
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="parentData"></param>
    /// /// <param name="variantData"></param>
    /// <param name="delta"></param>
    /// <param name="temperature"></param>
    /// <returns>Underflow</returns>
    protected FixedPoint2 ChangeVariantDataQuantity(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData parentData,
        ref SolutionComponent.VariantData variantData,
        FixedPoint2 delta,
        float? temperature)
    {
        var underflow = FixUnderflow(variantData.Quantity, ref delta);
        if (delta == 0)
            return underflow;
        variantData.Quantity += delta;
        parentData.TotalQuantity += delta;
        ChangeTotalVolume(solution,ref parentData, delta, temperature);
        Dirty(solution);
        return underflow;
    }

    protected FixedPoint2 ChangeAllReagentVariantDataQuantity(
        Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta,
        float? temp)
    {
        //If there are no variants, then just use the regular reagent method
        if (reagentData.Variants == null || reagentData.TotalQuantity == reagentData.Quantity)
            return ChangeReagentDataQuantity(solution, ref reagentData, delta, temp);

        //Start with the base reagent
        delta += ChangeReagentDataQuantity(solution,
            ref reagentData,
            ClampToEpsilon(delta.Float() / reagentData.VariantCount+1),
            temp);

        //shuffle the order we check variants so that they aren't always accessed in the
        //order of how old they are.
        var validCount = reagentData.VariantCount;
        var shuffle = CreateRandomIndexer(validCount);

        var skip = new bool[validCount];
        Array.Fill(skip, false);

        var variants = CollectionsMarshal.AsSpan(reagentData.Variants);
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
            var underflow = ChangeVariantDataQuantity(solution, ref reagentData, ref variantData, localDelta, temp);
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
        return ChangeReagentDataQuantity(solution, ref reagentData, delta, temp);
    }

    protected FixedPoint2 ChangeAllDataQuantities(
        Entity<SolutionComponent> solution,
        FixedPoint2 delta,
        float? temperature)
    {
        //create an indexShuffler so that reagents aren't accessed only by oldest
        var validCount = solution.Comp.Contents.Count;
        var shuffle = CreateRandomIndexer(solution.Comp.Contents.Count);
        var contents = CollectionsMarshal.AsSpan(solution.Comp.Contents);

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
            var underflow = ChangeAllReagentVariantDataQuantity(solution, ref reagentData, localDelta, temperature);
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
        float? temperature,
        bool force)
    {
        overflow = 0;
        if (!force && WillOverflow(solution, delta, out overflow))
            delta -= overflow;
        overflow -= ChangeReagentDataQuantity(solution, ref reagentData , delta, temperature);
    }

    protected void ChangeVariantQuantity_Implementation(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        ref SolutionComponent.VariantData variantData,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        float? temperature,
        bool force)
    {
        overflow = 0;
        if (!force && WillOverflow(solution, delta, out overflow))
            delta -= overflow;
        overflow -= ChangeVariantDataQuantity(solution, ref reagentData, ref variantData , delta, temperature);
    }

    protected void ChangeReagentTotalQuantity_Implementation(Entity<SolutionComponent> solution,
        ref SolutionComponent.ReagentData reagentData,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        float? tempDelta,
        bool force)
    {
        overflow = 0;
        WillOverflow(solution, delta, out overflow);
        if (!force)
            delta -= overflow;
        overflow = -ChangeAllReagentVariantDataQuantity(solution, ref reagentData, delta, tempDelta);
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
        ChangeTotalVolume(solution, ref reagentData, -reagentData.TotalQuantity, null);
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
        ref var reagentData = ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[variantData.ParentIndex];
        if (reagentData.Variants == null)
            return false;

        var i = 0;
        foreach (ref var variant in CollectionsMarshal.AsSpan(reagentData.Variants))
        {
            if (variant.Variant.Equals(variantData.Variant))
                break;
            i++;
        }

        if (i >= reagentData.Variants.Count)
            return false;

        reagentData.TotalQuantity -= variantData.Quantity;
        ChangeTotalVolume(solution, ref reagentData, variantData.Quantity, null);
        reagentData.Variants?.RemoveAt(i);
        solution.Comp.ReagentVariantCount--;
        if (removeBaseIfLast && reagentData.VariantCount == 0)
        {
            return PurgeReagent(solution, ref reagentData);
        }
        return true;
    }

    /// <summary>
    /// Completely removes all reagents from the solution
    /// </summary>
    /// <param name="solution">Target solution</param>
    /// <param name="resetTemperature"></param>
    protected void PurgeAllReagents(Entity<SolutionComponent> solution, bool resetTemperature = true)
    {
       solution.Comp.Contents.Clear();
       SetTotalVolume(solution, 0);
       ClearHeatCapacity(solution);
       ClearThermalEnergy(solution, resetTemperature);
       TrimAllocs(solution);
       Dirty(solution);
    }


    /// <summary>
    /// Ensures that reagentData is present for the specified reagent in the solution
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <returns>reagentData index</returns>
    protected int EnsureReagentData(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent)
    {
        var index = 0;
        foreach (ref var quantityData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (quantityData.ReagentId == reagent.Comp.Id)
                return index;
            index++;
        }
        index++;
        solution.Comp.Contents.Add(new SolutionComponent.ReagentData(reagent, 0, index));
        Dirty(solution);
        return index;
    }

    /// <summary>
    /// Ensures that variantData is present for the specified reagent in the solution
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <returns>variantData index</returns>
    protected int EnsureVariantData(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant)
    {
        var index = 0;
        ref var reagentData = ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[EnsureReagentData(solution, reagent)];
        foreach (ref var varData in CollectionsMarshal.AsSpan(reagentData.Variants))
        {
            if (varData.Variant.Equals(variant))
                return index;
            index++;
        }
        index++;
        reagentData.Variants ??= new(VariantAlloc);
        reagentData.Variants.Add(new SolutionComponent.VariantData(variant, 0, reagentData.Index));
        solution.Comp.ReagentVariantCount++;
        Dirty(solution);
        return index;
    }

    /// <summary>
    /// Tries to get the index of the specified reagent if it exists
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="index">Index of reagentData</param>
    /// <returns>If reagentData is valid</returns>
    protected bool TryGetReagentDataIndex(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        out int index)
    {
        index = 0;
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (reagentData.ReagentId == reagent.Comp.Id)
                return true;
            index++;
        }
        index = -1;
        return false;
    }

    /// <summary>
    /// Tries to get the index of the specified reagentVariant if it exists
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <param name="index">Index of variantData</param>
    /// <returns>If variantData is valid</returns>
    protected bool TryGetVariantDataIndex(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant,
        out int index)
    {
        if (!TryGetReagentDataIndex(solution, reagent, out var reagentIndex))
        {
            index = -1;
            return false;
        }
        index = 0;
        foreach (ref var variantData in CollectionsMarshal.AsSpan(
                     CollectionsMarshal.AsSpan(solution.Comp.Contents)[reagentIndex].Variants))
        {
            if (variantData.Variant.Equals(variant))
                return true;
            index++;
        }
        index = -1;
        return false;
    }

    #endregion

}
