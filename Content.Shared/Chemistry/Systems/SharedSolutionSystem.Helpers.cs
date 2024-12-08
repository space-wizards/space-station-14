

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    #region ReagentSpec

    [PublicAPI]
    public void EnsureReagent(Entity<SolutionComponent> solution,
        ReagentSpecifier reagent,
        bool logIfMissing = true)
    {
        if (!ResolveSpecifier(ref reagent, logIfMissing))
            return;
        EnsureReagent(solution, reagent.CachedDefinitionEntity!.Value, logIfMissing);
    }
    [PublicAPI]
    public bool TryGetReagentQuantity(
        Entity<SolutionComponent> solution,
        ReagentSpecifier reagent,
        out FixedPoint2 quantity,
        bool logIfMissing = true)
    {
        quantity = 0;
        return ResolveSpecifier(ref reagent, logIfMissing)
               && TryGetReagentQuantity(solution, reagent.CachedDefinitionEntity!.Value, out quantity, logIfMissing);
    }

    [PublicAPI]
    public bool TryGetTotalQuantity(
        Entity<SolutionComponent> solution,
        ReagentSpecifier reagent,
        out FixedPoint2 totalQuantity,
        bool logIfMissing = true)
    {
        totalQuantity = 0;
        return ResolveSpecifier(ref reagent, logIfMissing)
               && TryGetTotalQuantity(solution, reagent.CachedDefinitionEntity!.Value, out totalQuantity, logIfMissing);
    }

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        params ReagentSpecifier[] reagents)
    {
        var total = 0;
        foreach (var reagentSpec in reagents)
        {
            GetTotalQuantity(solution, reagentSpec, true);
        }
        return total;
    }

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        bool logIfMissing,
        params ReagentSpecifier[] reagents)
    {
        var total = 0;
        foreach (var reagentSpec in reagents)
        {
            GetTotalQuantity(solution, reagentSpec, logIfMissing);
        }
        return total;
    }

    [PublicAPI]
    public FixedPoint2 GetQuantity(Entity<SolutionComponent> solution,
        ReagentSpecifier reagent,
        ReagentVariant? variant = null,
        bool logIfMissing = true)
    {
        return !ResolveSpecifier(ref reagent, logIfMissing) ? 0 : GetQuantity(solution, reagent.CachedDefinitionEntity!.Value, variant,logIfMissing);
    }

    [PublicAPI]
    public ReagentQuantity GetReagentQuantity(Entity<SolutionComponent> solution,
        ReagentSpecifier reagent,
        bool logIfMissing = true)
    {
        return !ResolveSpecifier(ref reagent, logIfMissing)
            ? ReagentQuantity.Invalid
            : GetReagentQuantity(solution, reagent.CachedDefinitionEntity!.Value,logIfMissing);
    }

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        ReagentSpecifier reagent,
        bool logIfMissing = true)
    {
        return ResolveSpecifier(ref reagent, logIfMissing)
            ? GetTotalQuantity(solution, new ReagentDef(reagent.CachedDefinitionEntity!.Value, reagent.Variant))
            : 0;
    }

    #endregion

    #region ReagentQuantSpec

    [PublicAPI]
    public bool AddReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantitySpecifier reagentQuantity,
        out FixedPoint2 overflow,
        float? temperature = null,
        bool force = true,
        bool logIfMissing = true)
    {
        overflow = 0;
        return ReagentQuantitySpecifier.TryGetReagentQuantity(reagentQuantity, ChemistryRegistry,
                   out var reagentDef, logIfMissing)
               && AddReagent(solution, reagentDef, out overflow, temperature, force);
    }

    [PublicAPI]
    public void SetReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantitySpecifier reagentQuantity,
        out FixedPoint2 overflow,
        float? temperature = null,
        bool force = true,
        bool logIfMissing = true)
    {
        overflow = 0;
        if (!ReagentQuantitySpecifier.TryGetReagentQuantity(reagentQuantity, ChemistryRegistry, out var reagentDef,logIfMissing))
            return;
        SetReagent(solution, reagentDef, out overflow, temperature, force);
    }
    public bool RemoveReagent(
        Entity<SolutionComponent> solution,
        ReagentQuantitySpecifier reagentQuantity,
        out FixedPoint2 underFlow,
        float? temperature = null,
        bool force = true,
        bool purge = false,
        bool logIfMissing = true)
    {
        underFlow = 0;
        return ReagentQuantitySpecifier.TryGetReagentQuantity(reagentQuantity, ChemistryRegistry,
                   out var reagentDef, logIfMissing)
               && RemoveReagent(solution, reagentDef, out underFlow, temperature, force, purge);
    }

    #endregion

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        params string[] reagents)
    {
        FixedPoint2 total = 0;
        foreach (ref var reagentId in reagents.AsSpan())
        {
            total += GetTotalQuantity(solution, new ReagentSpecifier(reagentId));
        }
        return total;
    }

    public bool TryGetReagentDef(string id,
        out ReagentDef reagent,
        ReagentVariant? variant,
        bool logMissing = false)
    {
        return ChemistryRegistry.TryGetReagentDef(id, out reagent, variant, logMissing);
    }

    public bool TryGetReagentEntity(string id,
        [NotNullWhen(true)] out Entity<ReagentDefinitionComponent>? reagent,
        bool logMissing = false)
    {
        return ChemistryRegistry.TryGetReagentEntity(id, out reagent, logMissing);
    }

}
