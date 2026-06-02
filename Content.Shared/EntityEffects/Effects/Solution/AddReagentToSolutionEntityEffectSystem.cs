using Content.Shared.Chemistry.Components;
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
public sealed partial class AddReagentToSolutionEntityEffectSystem : EntityEffectSystem<SolutionManagerComponent, AddReagentToSolution>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void ApplyEffect(EntityUid target, EntityEffectData args)
    {
        if (args.Effect is not AddReagentToSolution typed)
            return;

        if (TryComp(target, out SolutionManagerComponent? mgr))
            Effect((target, mgr), typed, args);

        if (TryComp(target, out SolutionComponent? sol))
            Effect((target, sol), typed, args.Scale, args.User);
    }

    protected override void Effect(Entity<SolutionManagerComponent> entity, AddReagentToSolution effect, EntityEffectData data)
    {
        if (!_solutionContainer.TryGetSolution((entity, entity), effect.Solution, out var solutionContainer))
            return;

        _solutionContainer.TryAddReagent(solutionContainer.Value, effect.Reagent, data.Scale * effect.StrengthModifier);
    }

    private void Effect(Entity<SolutionComponent> entity, AddReagentToSolution effect, float scale, EntityUid? user)
    {
        if (entity.Comp.Id != effect.Solution)
            return;

        _solutionContainer.TryAddReagent(entity, effect.Reagent, scale * effect.StrengthModifier);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddReagentToSolution : EntityEffect
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
    public string Solution = "reagents";

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
