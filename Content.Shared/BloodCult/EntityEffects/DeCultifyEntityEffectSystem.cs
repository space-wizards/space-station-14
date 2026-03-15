using Robust.Shared.Audio.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.BloodCult;
using Content.Shared.Damage.Systems;

namespace Content.Shared.BloodCult.EntityEffects;

/// <summary>
/// Handles the effects when a cultist is deconverted
/// </summary>
public sealed partial class DeCultifyEntityEffectSystem : EntityEffectSystem<BloodCultistComponent, DeCultify>
{
	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly SharedStaminaSystem _stamina = default!;

	protected override void Effect(Entity<BloodCultistComponent> entity, ref EntityEffectEvent<DeCultify> args)
	{
		var bloodCultist = entity.Comp;
		var scale = args.Scale;

		var oldDeCultification = bloodCultist.DeCultification;
		var newDeCultification = oldDeCultification + (args.Effect.Amount * scale);
		bloodCultist.DeCultification = newDeCultification;
		Dirty(entity);

		// If this application causes deconversion (crosses 100 threshold), play sound and knock down
		if (oldDeCultification < 100.0f && newDeCultification >= 100.0f)
		{
			if (args.Effect.DeconversionSound != null)
				_audio.PlayPvs(args.Effect.DeconversionSound, entity, args.Effect.DeconversionSound.Params);

			// Apply stamina damage to knock them down (SharedStaminaSystem handles prediction)
			_stamina.TakeStaminaDamage(entity, args.Effect.DeconversionStaminaDamage, visual: false);
		}
	}
}
