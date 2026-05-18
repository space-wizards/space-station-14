using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AreaReactionEffect : EntityEffectBase<AreaReactionEffect>
{
    /// <summary>
    /// How many seconds will the effect stay, counting after fully spreading.
    /// </summary>
    [DataField("duration")] public float Duration = 10;

    /// <summary>
    /// How big of a reaction scale we need for 1 smoke entity.
    /// </summary>
    [DataField] public float OverflowThreshold = 2.5f;

    /// <summary>
    /// The entity prototype that is being spread over an area.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId PrototypeId;

    /// <summary>
    /// Sound that will get played when this reaction effect occurs.
    /// </summary>
    [DataField(required: true)] public SoundSpecifier Sound = default!;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-area-reaction",
            ("duration", Duration)
        );

    public override LogImpact? Impact => LogImpact.High;
}
