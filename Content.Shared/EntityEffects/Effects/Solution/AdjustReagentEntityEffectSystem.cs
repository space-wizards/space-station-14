using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

/// <summary>
/// Adjust a reagent in this solution by an amount modified by scale.
/// Quantity is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustReagentEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustReagent>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AdjustReagent> args)
    {
        var quantity = args.Effect.Amount * args.Scale;
        var reagent = args.Effect.Reagent;

        if (quantity > 0)
            _solutionContainer.TryAddReagent(entity, reagent, quantity);
        else
            _solutionContainer.RemoveReagent(entity, reagent, -quantity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
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
