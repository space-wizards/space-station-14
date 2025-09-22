using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

/// <summary>
/// This is used for...
/// </summary>
public abstract partial class SharedAreaReactionEntityEffectsSystem : EntityEffectSystem<SolutionComponent, AreaReactionEffect>
{
    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AreaReactionEffect> args)
    {
        // Server side effect
    }
}

public sealed partial class AreaReactionEffect : EntityEffectBase<AreaReactionEffect>
{
    /// <summary>
    /// How many seconds will the effect stay, counting after fully spreading.
    /// </summary>
    [DataField("duration")] public float Duration = 10;

    // TODO: WE MAY BE ABLE TO REMOVE THIS AND JUST PASS SCALE PRECALCULATED???
    /// <summary>
    /// How big of a reaction scale we need for 1 smoke entity.
    /// </summary>
    [DataField] public float OverflowThreshold = 2.5f;

    /// <summary>
    /// The entity prototype that will be spawned as the effect.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId PrototypeId;

    /// <summary>
    /// Sound that will get played when this reaction effect occurs.
    /// </summary>
    [DataField(required: true)] public SoundSpecifier Sound = default!;

    public override bool ShouldLog => true;

    public override LogImpact LogImpact => LogImpact.High;
}
