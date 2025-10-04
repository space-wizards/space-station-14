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

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return prototype.Resolve(Reagent, out ReagentPrototype? proto)
            ? Loc.GetString("entity-effect-guidebook-adjust-reagent-reagent",
                ("chance", Probability),
                ("deltasign", MathF.Sign(Amount.Float())),
                ("reagent", proto.LocalizedName),
                ("amount", MathF.Abs(Amount.Float())))
            : null;
    }
}
