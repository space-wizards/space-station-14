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

	public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
		=> Loc.GetString("entity-effect-guidebook-de-cultify", ("chance", Probability), ("amount", Amount));
}
