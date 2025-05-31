using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

[DataDefinition]
public sealed partial class FlashReactionEffect : EventEntityEffect<FlashReactionEffect>
{
    /// <summary>
    ///     Flash range per unit of reagent.
    /// </summary>
    [DataField]
    public float RangePerUnit = 0.2f;

    /// <summary>
    ///     Maximum flash range.
    /// </summary>
    [DataField]
    public float MaxRange = 10f;

    /// <summary>
    ///     How much to entities are slowed down.
    /// </summary>
    [DataField]
    public float SlowTo = 0.5f;

    /// <summary>
    ///     The time entities will be flashed in seconds.
    ///     The default is chosen to be better than the hand flash so it is worth using it for grenades etc.
    /// </summary>
    [DataField]
    public float Duration = 4f;

    /// <summary>
    ///     The prototype ID used for the visual effect.
    /// </summary>
    [DataField]
    public EntProtoId? FlashEffectPrototype = "ReactionFlash";

    /// <summary>
    ///     The sound the flash creates.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-flash-reaction-effect", ("chance", Probability));
}
