using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Types;
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
        Entity<ReagentDefinitionComponent> reagent)
    {
        EnsureReagentRef(solution, reagent, out _);
    }

    [PublicAPI]
    public void EnsureReagent(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant)
    {
        EnsureReagentRef(solution, reagent, variant ,out _);
    }

    /// <summary>
    /// Attempt to get the quantity of a reagent
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="reagentQuantity"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetReagentQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        out FixedPoint2 reagentQuantity)
    {
        ref var quantityData = ref GetQuantityRef(solution, reagent);
        if (!IsValidQuantity(ref quantityData))
        {
            reagentQuantity = -1;
            return false;
        }
        reagentQuantity = quantityData.Quantity;
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <param name="variantQuantity"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetReagentQuantity(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant,
        out FixedPoint2 variantQuantity)
    {
        ref var quantityData = ref GetQuantityRef(solution, reagent, variant);
        if (!IsValidQuantity(ref quantityData))
        {
            variantQuantity = -1;
            return false;
        }
        variantQuantity = quantityData.Quantity;
        return true;
    }


    /// <summary>
    /// Get the quantity of a reagent, throwing an exception if the reagent is not found
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    [PublicAPI]
    public FixedPoint2 GetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent)
    {
        if (!TryGetReagentQuantity(solution, reagent, out var quantity))
            throw new KeyNotFoundException($"Reagent:{reagent.Comp.Id} was not found in solution:{ToPrettyString(solution)}");
        return quantity;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="variant"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    [PublicAPI]
    public FixedPoint2 GetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant)
    {
        if (!TryGetReagentQuantity(solution, reagent, variant, out var quantity))
            throw new KeyNotFoundException($"Reagent:{reagent.Comp.Id} was not found in solution:{ToPrettyString(solution)}");
        return quantity;
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
        if (variant == null)
        {
            ChangeQuantity(solution, ref EnsureReagentRef(solution, reagent, out _), quantity);
            return;
        }
        ChangeQuantity(solution, ref EnsureReagentRef(solution, reagent, variant, out _), quantity);
    }

    /// <summary>
    /// Add an amount of reagent to a solution, respecting maxVolume!
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagent"></param>
    /// <param name="quantity"></param>
    /// <param name="overflow"></param>
    /// <param name="preventOverflow">Should we only output the overflow or prevent it</param>
    /// <param name="variant"></param>
    [PublicAPI]
    public void AddReagent(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        out FixedPoint2 overflow,
        bool preventOverflow = true,
        ReagentVariant? variant = null)
    {
        var newVolume = FixedPoint2.Max(solution.Comp.Volume + quantity,0);
        overflow = FixedPoint2.Max(newVolume - solution.Comp.Volume, 0);
        if (preventOverflow)
            quantity -= overflow;
        AddReagent(solution, reagent, quantity, variant);
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
        if (variant == null)
        {
            var contents = CollectionsMarshal.AsSpan(solution.Comp.Contents);
            foreach (ref var quantData in contents)
            {
                if (quantData.ReagentId != reagent.Comp.Id)
                    continue;
                ChangeQuantity(solution, ref quantData, -quantity);
                return true;
            }
            return false;
        }
        var variantContents = CollectionsMarshal.AsSpan(solution.Comp.VariantContents);
        foreach (ref var quantData in variantContents)
        {
            if (quantData.ReagentId != reagent.Comp.Id)
                continue;
            ChangeQuantity(solution, ref quantData, -quantity);
            return true;
        }
        return false;
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
        SetQuantity(solution, ref EnsureReagentRef(solution, reagent, out _), newQuantity);
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
        SetQuantity(solution, ref EnsureReagentRef(solution, reagent, variant, out _), newQuantity);
    }

    public IEnumerable<(Entity<ReagentDefinitionComponent> reagent, FixedPoint2 quantity)>
        EnumerateReagents(Entity<SolutionComponent> solution)
    {
        foreach (var reagentQuantData in solution.Comp.Contents)
        {
            var reagentDef = reagentQuantData.ReagentDef!.Value;
            yield return (reagentDef, reagentQuantData.Quantity);
        }
    }

    public IEnumerable<(Entity<ReagentDefinitionComponent> reagent, FixedPoint2 quantity)>
        EnumerateReagentVariants(Entity<SolutionComponent> solution)
    {
        foreach (var reagentQuantData in solution.Comp.VariantContents)
        {
            var reagentDef = reagentQuantData.ReagentDef!.Value;
            yield return (reagentDef, reagentQuantData.Quantity);
        }
    }

    public ReagentQuantity? GetPrimaryReagent(Entity<SolutionComponent> solution)
    {
        return solution.Comp.PrimaryReagentIndex < 0 ? null : solution.Comp.Contents[solution.Comp.PrimaryReagentIndex];
    }

    public bool TryGetPrimaryReagent(Entity<SolutionComponent> solution,
        [NotNullWhen(true)] out ReagentQuantity? reagent)
    {
        reagent = GetPrimaryReagent(solution);
        return reagent != null;
    }

    #endregion



    #region Internal

    /// <summary>
    /// Change the quantity of a reagent and automatically adjusts variant quantities to match (if enabled)
    /// </summary>
    /// <param name="solution">Solution that contains the ReagentQuantity</param>
    /// <param name="storedQuantity">ReagentQuantity to modify</param>
    /// <param name="delta">Change in Quantity</param>
    /// <param name="changeVariants">Should variant quantities be adjusted as well</param>
    protected void ChangeQuantity(Entity<SolutionComponent> solution,
        ref ReagentQuantity storedQuantity,
        FixedPoint2 delta,
        bool changeVariants = false)
    {
        if (!changeVariants)
        {
            storedQuantity.Quantity = FixedPoint2.Max(storedQuantity.Quantity + delta, 0);
            ChangeTotalVolume(solution, delta);
            return;
        }
        SetQuantity(solution, ref storedQuantity, storedQuantity.Quantity+delta);
    }

    /// <summary>
    /// Change the quantity of a reagent variant
    /// </summary>
    /// <param name="solution">Solution that contains the ReagentQuantity</param>
    /// <param name="storedQuantity">ReagentVariantQuantity to modify</param>
    /// <param name="delta">Change in Quantity</param>
    protected void ChangeQuantity(Entity<SolutionComponent> solution,
        ref ReagentVariantQuantity storedQuantity,
        FixedPoint2 delta)
    {
        SetQuantity(solution, ref storedQuantity, storedQuantity.Quantity+delta);
    }


    /// <summary>
    /// Sets the quantity of the specified reagent
    /// </summary>
    /// <param name="solution">Solution that contains the ReagentQuantity</param>
    /// <param name="storedQuantity">ReagentQuantity to modify</param>
    /// <param name="quantity">New quantity</param>
    protected void SetQuantity(Entity<SolutionComponent> solution,
        ref ReagentQuantity storedQuantity,
        FixedPoint2 quantity)
    {
        if (storedQuantity.Quantity == quantity)
            return;
        quantity = FixedPoint2.Max(quantity, 0);
        var delta = quantity - storedQuantity.Quantity;
        storedQuantity.Quantity = FixedPoint2.Max(0,storedQuantity.Quantity + delta);
        ChangeTotalVolume(solution, delta);
        if (storedQuantity.VariantIndices == null || storedQuantity.VariantIndices.Count == 0)
            return;
        var variantContents = CollectionsMarshal.AsSpan(solution.Comp.VariantContents);
        var perDelta = delta / solution.Comp.VariantContents.Count;
        FixedPoint2 counter = 0;
        if (perDelta != 0)
        {
            foreach (var vi in storedQuantity.VariantIndices)
            {
                ref var variantData = ref variantContents[vi];
                variantData.Quantity = FixedPoint2.Max(0, variantData.Quantity + perDelta);
            }
        }
        else
        {
            foreach (var vi in storedQuantity.VariantIndices)
            {
                ref var variantData = ref variantContents[vi];
                if (counter >= perDelta)
                    break;
                variantData.Quantity = FixedPoint2.Max(0, variantData.Quantity + FixedPoint2.Epsilon);
                counter += FixedPoint2.Epsilon;
            }
        }
    }

    /// <summary>
    /// Sets the quantity of the specified reagent
    /// </summary>
    /// <param name="solution">Solution that contains the ReagentQuantity</param>
    /// <param name="storedQuantity">ReagentVariantQuantity to modify</param>
    /// <param name="quantity">New quantity</param>
    protected void SetQuantity(Entity<SolutionComponent> solution,
        ref ReagentVariantQuantity storedQuantity,
        FixedPoint2 quantity)
    {
        if (storedQuantity.Quantity == quantity)
            return;
        var contents = CollectionsMarshal.AsSpan(solution.Comp.Contents);
        ref var reagentData = ref contents[storedQuantity.CachedReagentIndex];
        quantity = FixedPoint2.Max(quantity, 0);
        var delta = quantity - storedQuantity.Quantity;
        reagentData.Quantity = FixedPoint2.Max(0, reagentData.Quantity + delta);
        ChangeTotalVolume(solution, delta);
    }

    protected ref ReagentQuantity GetQuantityRef(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent)
    {
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (reagent.Comp.Id != reagentData.ReagentId)
                continue;
            return ref reagentData;
        }
        return ref InvalidQuantity;
    }

    protected ref ReagentVariantQuantity GetQuantityRef(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant)
    {
        foreach (ref var variantData in CollectionsMarshal.AsSpan(solution.Comp.VariantContents))
        {
            if (variantData.Variant == null
                || reagent.Comp.Id != variantData.ReagentId
                || !variantData.Variant.Equals(variant))
                continue;
            return ref variantData;
        }
        return ref InvalidVariantQuantity;
    }


    /// <summary>
    /// Ensures that the specified reagent will be present in the solution
    /// </summary>
    /// <param name="solution">target solution</param>
    /// <param name="reagent">reagent to add</param>
    /// <param name="index">index of the found/added reagent</param>
    /// <returns>reference to reagent quantity data</returns>
    protected ref ReagentQuantity EnsureReagentRef(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        out int index)
    {
        index = 0;
        foreach (ref var quantData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (quantData.ReagentId == reagent.Comp.Id)
                return ref quantData;
            index++;
        }
        AddQuantityData(solution, new ReagentQuantity(reagent, 0));
        return ref CollectionsMarshal.AsSpan(solution.Comp.Contents)[index];
    }

    /// <summary>
    /// Ensures that the specified reagent will be present in the solution
    /// </summary>
    /// <param name="solution">target solution</param>
    /// <param name="reagent">reagent to add</param>
    /// <param name="variant">reagent variant to add</param>
    /// <param name="index">index of the found/added reagent</param>
    /// <returns>reference to reagent quantity data</returns>
    protected ref ReagentVariantQuantity EnsureReagentRef(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        ReagentVariant variant,
        out int index)
    {
        index = 0;
        ref var reagentData = ref EnsureReagentRef(solution, reagent, out var reagentIndex);
        var variantContents = CollectionsMarshal.AsSpan(solution.Comp.VariantContents);
        if (reagentData.VariantIndices != null)
        {
            foreach (var vi in reagentData.VariantIndices)
            {
                ref var possibleVariant = ref variantContents[vi];
                if (possibleVariant.Variant == null || !possibleVariant.Variant.Equals(variant))
                    continue;
                index = vi;
                return ref possibleVariant;
            }
        }
        AddQuantityData(solution, new ReagentVariantQuantity(reagent, variant, reagentIndex, 0));
        index = solution.Comp.VariantContents.Count - 1;
        reagentData.VariantIndices = [index];
        return ref CollectionsMarshal.AsSpan(solution.Comp.VariantContents)[index];
    }


    #endregion

}
