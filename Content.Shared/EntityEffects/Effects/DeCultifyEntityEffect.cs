using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DeCultify : EntityEffectBase<DeCultify>
{
	/// <summary>
	/// Amount of de-cultification to apply per effect trigger.
	/// </summary>
	[DataField]
	public float Amount = 10.0f;

	/// <summary>
	/// Sound played when deconversion is triggered
	/// </summary>
	[DataField]
	public SoundSpecifier? DeconversionSound = new SoundPathSpecifier("/Audio/Effects/holy.ogg");

	/// <summary>
	/// Stamina damage applied when deconversion is triggered (knocks the cultist down).
	/// </summary>
	[DataField]
	public float DeconversionStaminaDamage = 100f;

	public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
		=> Loc.GetString("entity-effect-guidebook-de-cultify", ("chance", Probability), ("amount", Amount));
}
