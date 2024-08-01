using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
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
        EnsureReagentDataRef(solution, reagent, variant);
    }

    /// <summary>
    /// Ensures that the specified reagent will be present in the solution
    /// </summary>
    /// <param name="solution">target solution</param>
    /// <param name="reagent">reagent to add</param>
    /// <param name="variant"></param>
    [PublicAPI]
    public void EnsureReagent(Entity<SolutionComponent> solution,
        ReagentDef reagent,
        ReagentVariant? variant = null
        )
    {
        if (!ChemistryRegistry.ResolveReagent(ref reagent))
            return;
        EnsureReagent(solution, reagent.DefinitionEntity, variant);
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
    public bool TryGetQuantity(
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
            quantity = reagentData.BaseQuantity;
            return true;
        }
        ref var variantData = ref GetReagentDataRef(solution, reagent, variant);
        if (!variantData.IsValid)
            return false;
        quantity = variantData.Quantity;
        return true;
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
    public bool TryGetQuantity(
        Entity<SolutionComponent> solution,
        ReagentDef reagent,
        out FixedPoint2 quantity,
        ReagentVariant? variant = null)
    {
        quantity = 0;
        return ChemistryRegistry.ResolveReagent(ref reagent)
               && TryGetQuantity(solution, reagent.DefinitionEntity, out quantity, variant);
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
        if (!TryGetQuantity(solution, reagent, out var quantity, variant))
            return -1;
        return quantity;
    }

    /// <summary>
    ///
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
        if (!TryGetQuantity(solution, reagent, out var quantity, variant))
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
        return !TryGetQuantity(solution, reagent, out var quantity, variant)
            ? new ReagentQuantity()
            : new ReagentQuantity(new ReagentDef(reagent, variant), quantity);
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
        ReagentDef reagent,
        ReagentVariant? variant = null)
    {
        return !ChemistryRegistry.ResolveReagent(ref reagent)
            ? new ReagentQuantity()
            : GetReagentQuantity(solution, reagent.DefinitionEntity, variant);
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
        return EnumerateReagentVariants<ReagentVariant>(solution, true);
    }

    /// <summary>
    /// Enumerates only ReagentVariants of the specified type
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="includeChildTypes"></param>
    /// <returns></returns>
    [PublicAPI]
    public IEnumerable<ReagentDef> EnumerateReagentVariants<T>(Entity<SolutionComponent> solution,
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
    /// Add an amount of reagent to a solution, this will ignore maxVolume!
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="quantity"></param>
    /// <param name="variant"></param>
    [PublicAPI]
    public void AddReagent(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        ReagentVariant? variant = null)
    {
    }

    /// <summary>
    /// Tries to remove the specified amount of reagent
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="quantity"></param>
    /// <param name="variant"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool RemoveReagent(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        ReagentVariant? variant = null)
    {

    }

    /// <summary>
    /// Set the quantity of the target reagent, this will scale variant quantities accordingly. This will ignore MaxVolume!
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="newQuantity"></param>
    [PublicAPI]
    public void SetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 newQuantity)
    {

    }

    /// <summary>
    /// Sets the quantity of the target reagent variant. This will ignore MaxVolume!
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <param name="newQuantity"></param>
    [PublicAPI]
    public void SetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant,
        FixedPoint2 newQuantity)
    {
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
    /// Ensures that reagentData is present for the specified reagent in the solution
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <returns>Reference to variantData</returns>
    protected ref SolutionComponent.VariantData EnsureReagentDataRef(
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
    protected ref SolutionComponent.VariantData GetReagentDataRef(Entity<SolutionComponent> solution,
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
        ChangeReagentVolume(solution, ref reagentData, -reagentData.TotalQuantity);
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
        ChangeReagentVolume(solution, ref reagentData, variantData.Quantity);
        reagentData.Variants?.RemoveAt(i);
        if (removeBaseIfLast && reagentData.Variants?.Count == 0)
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
