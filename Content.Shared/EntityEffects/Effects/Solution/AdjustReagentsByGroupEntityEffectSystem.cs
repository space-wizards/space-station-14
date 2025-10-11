using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

/// <summary>
/// Adjust all reagents in this solution which are metabolized by a given metabolism group.
/// Quantity is modified by scale, quantity is per reagent and not a total.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustReagentsByGroupEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustReagentsByGroup>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AdjustReagentsByGroup> args)
    {
        var amount = args.Effect.Amount * args.Scale;
        var group = args.Effect.Group;
        var solution = entity.Comp.Solution;

        foreach (var quant in solution.Contents.ToArray())
        {
            var proto = _proto.Index<ReagentPrototype>(quant.Reagent.Prototype);
            if (proto.Metabolisms == null || !proto.Metabolisms.ContainsKey(group))
                continue;

            if (amount < 0)
                solution.RemoveReagent(quant.Reagent, -amount);
            else
                solution.AddReagent(quant.Reagent, amount);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustReagentsByGroup : EntityEffectBase<AdjustReagentsByGroup>
{

    /// <summary>
    ///     The metabolism group being adjusted. All reagents in an affected solution with this group will be adjusted.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MetabolismGroupPrototype> Group;

    [DataField(required: true)]
    public FixedPoint2 Amount;
}
