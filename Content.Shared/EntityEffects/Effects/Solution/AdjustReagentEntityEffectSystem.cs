using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

public sealed partial class AdjustReagentEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustReagent>
{
    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AdjustReagent> args)
    {
        var amount = args.Effect.Amount * args.Scale;
        var reagent = args.Effect.Reagent;
        var solution = entity.Comp.Solution;

        if (amount < 0)
            solution.RemoveReagent(reagent, -amount);
        else
            solution.AddReagent(reagent, amount);
    }
}

public sealed partial class AdjustReagent : EntityEffectBase<AdjustReagent>
{
    /// <summary>
    ///     The reagent ID to add or remove.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public FixedPoint2 Amount;
}
