using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Adds a reagent directly to the target's bloodstream.
/// Quantity is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AddReagentToBloodstreamEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, AddReagentToBloodstream>
{
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private ReactiveSystem _reactive = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, AddReagentToBloodstream effect, EntityEffectData data)
    {
        var solution = new Content.Shared.Chemistry.Components.Solution();
        solution.AddReagent(effect.Reagent, effect.Quantity * data.Scale);

        _bloodstream.TryAddToBloodstream(entity.AsNullable(), solution);
        _reactive.DoEntityReaction(entity, solution, ReactionMethod.Injection); // TODO: This should be part of TryAddToBloodstream
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddReagentToBloodstream : EntityEffect
{
    /// <summary>
    /// Prototype of the reagent we're adding.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>
    /// Amount of reagent to add to the bloodstream.
    /// </summary>
    [DataField]
    public FixedPoint2 Quantity = 1.0;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return prototype.Resolve(Reagent, out var proto)
            ? Loc.GetString("entity-effect-guidebook-add-reagent-to-bloodstream", ("chance", Probability), ("reagent", proto.LocalizedName), ("quantity", Quantity))
            : null;
    }
}
