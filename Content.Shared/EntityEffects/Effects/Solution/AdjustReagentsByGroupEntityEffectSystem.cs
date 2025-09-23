using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

// TODO: Metabolism Groups suck and also I'm not even sure if this is even *used* so it should be removed either now or in the future.
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
