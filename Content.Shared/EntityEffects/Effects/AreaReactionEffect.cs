using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Basically smoke and foam reactions.
/// </summary>
public sealed partial class AreaReactionEffect : EventEntityEffect<AreaReactionEffect>
{
    /// <summary>
    /// How many seconds will the effect stay, counting after fully spreading.
    /// </summary>
    [DataField("duration")] public float Duration = 10;

    /// <summary>
    /// How many units of reaction for 1 smoke entity.
    /// </summary>
    [DataField] public FixedPoint2 OverflowThreshold = FixedPoint2.New(2.5);

    /// <summary>
    /// The entity prototype that will be spawned as the effect.
    /// </summary>
    [DataField("prototypeId", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PrototypeId = default!;

    /// <summary>
    /// Sound that will get played when this reaction effect occurs.
    /// </summary>
    [DataField("sound", required: true)] public SoundSpecifier Sound = default!;

    public override bool ShouldLog => true;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-area-reaction",
                    ("duration", Duration)
                );

    public override LogImpact LogImpact => LogImpact.High;
}
