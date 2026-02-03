using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

// TODO: This should be removed and changed to an "AbsorbentSolutionComponent"
/// <summary>
/// Creates a reagent in a specified solution owned by this entity.
/// Quantity is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed class AddReagentToSolutionEntityEffectSystem : EntityEffectSystem<SolutionContainerManagerComponent, AddReagentToSolution>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionContainerManagerComponent> entity, ref EntityEffectEvent<AddReagentToSolution> args)
    {
        var solution = args.Effect.Solution;
        var reagent = args.Effect.Reagent;

        if (!_solutionContainer.TryGetSolution((entity, entity), solution, out var solutionContainer))
            return;

        _solutionContainer.TryAddReagent(solutionContainer.Value, reagent, args.Scale * args.Effect.StrengthModifier);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddReagentToSolution : EntityEffectBase<AddReagentToSolution>
{
    /// <summary>
    ///     Prototype of the reagent we're adding.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    ///<summary>
    ///     Solution we're looking for
    /// </summary>
    [DataField(required: true)]
    public string? Solution = "reagents";

    ///<summary>
    ///     A modifier for how much reagent we're creating.
    /// </summary>
    [DataField]
    public float StrengthModifier = 1.0f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return prototype.Resolve(Reagent, out ReagentPrototype? proto)
            ? Loc.GetString("entity-effect-guidebook-add-to-solution-reaction",
                ("chance", Probability),
                ("reagent", proto.LocalizedName))
            : null;
    }
}
