// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server.BloodCult.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
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
			// Play holy sound
			_audio.PlayPvs(
				new SoundPathSpecifier("/Audio/Effects/holy.ogg"),
				entity,
				AudioParams.Default
			);

			// Apply stamina damage to knock them down
			_stamina.TakeStaminaDamage(entity, 100f, visual: false);
		}
	}
}
