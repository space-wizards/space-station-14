using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.NewEffects.Solution;

public sealed class AddReagentToSolutionEntityEffectSystem : EntityEffectSystem<SolutionContainerManagerComponent, AddReagentToSolution>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionContainerManagerComponent> entity, ref EntityEffectEvent<AddReagentToSolution> args)
    {
        var solution = args.Effect.Solution;
        var reagent = args.Effect.Reagent;

        if (!_solutionContainer.TryGetSolution((entity, entity), solution, out var solutionContainer))
            return;

        // TODO: Make sure this properly removes reagents if we're using reagents!!!
        _solutionContainer.TryAddReagent(solutionContainer.Value, reagent, args.Scale, out var accepted);
    }
}

public sealed partial class AddReagentToSolution : EntityEffectBase<AddReagentToSolution>
{
    /// <summary>
    ///     Amount of firestacks reduced.
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
    [DataField(required: true)]
    public float StrengthModifier = 1.0f;
}
