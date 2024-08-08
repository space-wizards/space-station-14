using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{

    #region RemovingOuts

    /// <inheritdoc cref="SharedSolutionSystem.SetReagentQuantity" />
    [PublicAPI]
    public void SetReagent(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 newQuantity,
        ReagentVariant? variant = null,
        bool force = true)
    {
        SetReagent(solution, reagent, newQuantity, out _,variant, force);
    }

    /// <inheritdoc cref="SharedSolutionSystem.ChangeReagentQuantity" />
    [PublicAPI]
    public bool AddReagent(
        Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity,
        ReagentVariant? variant = null,
        bool force = true)
    {
        return AddReagent(solution, reagent, quantity, out _ ,variant, force);
    }

    #endregion

    #region ReagentDef
    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        ReagentDef reagent)
    {
        if (ResolveReagent(ref reagent))
            return 0;
        FixedPoint2 totalQuantity = 0;
        if (!TryGetReagentDataIndex(solution, reagent.DefinitionEntity, out var index))
            return totalQuantity;
        totalQuantity = CollectionsMarshal.AsSpan(solution.Comp.Contents)[index].TotalQuantity;
        return totalQuantity;
    }

    /// <inheritdoc cref="SharedSolutionSystem.EnsureReagent"/>
    [PublicAPI]
    public void EnsureReagent(Entity<SolutionComponent> solution,
        ReagentDef reagent,
        ReagentVariant? variant = null
    )
    {
        if (ResolveReagent(ref reagent))
            return;
        EnsureReagent(solution, reagent.DefinitionEntity, variant);
    }

    /// <inheritdoc cref="SharedSolutionSystem.TryGetReagentQuantity"/>
    [PublicAPI]
    public bool TryGetReagentQuantity(
        Entity<SolutionComponent> solution,
        ReagentDef reagent,
        out FixedPoint2 quantity,
        ReagentVariant? variant = null)
    {
        quantity = 0;
        return ResolveReagent(ref reagent) && TryGetReagentQuantity(solution, reagent.DefinitionEntity, out quantity, variant);
    }

    /// <inheritdoc cref="SharedSolutionSystem.TryGetTotalQuantity"/>
    [PublicAPI]
    public bool TryGetTotalQuantity(
        Entity<SolutionComponent> solution,
        ReagentDef reagent,
        out FixedPoint2 totalQuantity)
    {
        totalQuantity = 0;
        return ResolveReagent(ref reagent) && TryGetTotalQuantity(solution, reagent.DefinitionEntity, out totalQuantity);
    }

    /// <inheritdoc cref="SharedSolutionSystem.SetReagentQuantity"/>
    [PublicAPI]
    public ReagentQuantity GetReagentQuantity(Entity<SolutionComponent> solution,
        ReagentDef reagent,
        ReagentVariant? variant = null)
    {
        return ResolveReagent(ref reagent)
            ? new ReagentQuantity()
            : GetReagentQuantity(solution, reagent.DefinitionEntity, variant);
    }

    /// <inheritdoc cref="SharedSolutionSystem.SetReagentQuantity"/>
    [PublicAPI]
    public void SetReagent(
        Entity<SolutionComponent> solution,
        ReagentDef reagent,
        FixedPoint2 newQuantity,
        ReagentVariant? variant = null)
    {
        if (!ResolveReagent(ref reagent))
            return;
        SetReagent(solution, reagent.DefinitionEntity, newQuantity, variant);
    }

    #endregion

    #region ReagentQuantity

    /// <inheritdoc cref="SharedSolutionSystem.SetReagentQuantity"/>
    [PublicAPI]
    public void SetReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantity quantity)
    {
        if (!ResolveReagent(ref quantity))
            return;
        SetReagent(solution, quantity.ReagentDef.DefinitionEntity, quantity.Quantity, quantity.ReagentDef.Variant);
    }

    public bool AddReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantity quantity,
        out FixedPoint2 overflow,
        bool force = true)
    {
        overflow = 0;
        return ResolveReagent(ref quantity)
               && AddReagent(solution, quantity.ReagentDef.DefinitionEntity, quantity.Quantity,
                   out overflow, quantity.ReagentDef.Variant, force);
    }


    [PublicAPI]
    public bool RemoveReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantity quantity,
        out FixedPoint2 underFlow,
        bool force = true,
        bool purge = false)
    {
        underFlow = 0;
        return ResolveReagent(ref quantity)
               && RemoveReagent(solution, quantity.ReagentDef.DefinitionEntity, quantity.Quantity,
                   out underFlow, quantity.ReagentDef.Variant, force, purge);
    }

    #endregion

}
