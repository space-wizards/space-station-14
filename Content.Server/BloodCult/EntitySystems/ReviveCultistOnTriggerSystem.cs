// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Trigger;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.BloodCult;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Administration.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Administration.Systems;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.BloodCult.EntitySystems
{
	public sealed partial class ReviveCultistOnTriggerSystem : EntitySystem
	{
		[Dependency] private readonly SharedAudioSystem _audioSystem = default!;
		[Dependency] private readonly DamageableSystem _damageableSystem = default!;
		[Dependency] private readonly PopupSystem _popupSystem = default!;

		[Dependency] private readonly EntityLookupSystem _lookup = default!;
		[Dependency] private readonly MobStateSystem _mobState = default!;
		[Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
		[Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;
		[Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
		[Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

		public override void Initialize()
		{
			base.Initialize();
			SubscribeLocalEvent<ReviveCultistOnTriggerComponent, TriggerEvent>(HandleReviveTrigger);
		}

	private void HandleReviveTrigger(EntityUid uid, ReviveCultistOnTriggerComponent component, TriggerEvent args)
	{
		if (args.User == null || !HasComp<BloodCultistComponent>(args.User))
			return;

		EntityUid user = (EntityUid)args.User;
		var lookup = _lookup.GetEntitiesInRange(uid, component.ReviveRange);
		
		// Find dead entities to revive (prioritize cultists, but can revive anyone)
		foreach (var look in lookup)
		{
			// Skip the user themselves and living entities
			if (look == user || !_mobState.IsDead(look))
				continue;

			// Check if entity has a mind (has a soul to revive)
			EntityUid? mindId = CompOrNull<MindContainerComponent>(look)?.Mind;
			if (mindId == null)
				continue;

			bool isCultist = HasComp<BloodCultistComponent>(look);

		// Extract blood from the revived entity (only for non-cultists)
		double bloodExtracted = 0.0;
		if (!isCultist && TryComp<BloodstreamComponent>(look, out var bloodstream))
		{
			// Resolve the blood solution and get its volume
			if (_solutionContainer.ResolveSolution(look, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
			{
				bloodExtracted = (double)bloodSolution.Volume.Float();
				
				// Remove all blood from the entity
				_solutionContainer.RemoveAllSolution(bloodstream.BloodSolution.Value);
			}
		}

			// Revive the entity (no health cost to the user)
			_rejuvenate.PerformRejuvenate(look);
			
			// Apply different conditions based on cultist status
			if (isCultist)
			{
				// Cultists are revived with 20 health remaining
				// Get the death threshold (which represents max health) and calculate damage needed
				if (TryComp<MobThresholdsComponent>(look, out var thresholds) &&
				    _mobThreshold.TryGetDeadThreshold(look, out var maxHealth, thresholds) &&
				    maxHealth != null)
				{
					// Calculate damage needed: maxHealth - 80
					var damageNeeded = maxHealth.Value - FixedPoint2.New(80);
					if (damageNeeded > FixedPoint2.Zero)
					{
						var revivalDamage = new DamageSpecifier();
						revivalDamage.DamageDict.Add("Bloodloss", damageNeeded);
						_damageableSystem.TryChangeDamage(look, revivalDamage, true, origin: user);
					}
				}
			}
			else
			{
				// Non-cultists are revived to critical/incapacitated state
				// Apply enough damage to put them in critical but not dead
				if (TryComp<DamageableComponent>(look, out var damageable))
				{
					var criticalDamage = new DamageSpecifier();
					criticalDamage.DamageDict.Add("Slash", FixedPoint2.New(140)); // Just enough to put in critical
					_damageableSystem.TryChangeDamage(look, criticalDamage, true, origin: user);
				}
			}
			
			// Play effects
			_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Magic/staff_healing.ogg"), Transform(look).Coordinates);
			
			// Use different messages for cultists vs non-cultists
			if (isCultist)
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-revive-success"),
					user, user, PopupType.Large
				);
			}
			else
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-revive-success-noncultist"),
					user, user, PopupType.Large
				);
			}

			// Add extracted blood to the ritual pool (only for non-cultists)
			if (!isCultist && bloodExtracted > 0)
			{
				_bloodCultRule.AddBloodForConversion(bloodExtracted);
			}

			args.Handled = true;
			return;
		}

		// No valid target found
		_popupSystem.PopupEntity(
			Loc.GetString("cult-revive-fail-notarget"),
			user, user, PopupType.MediumCaution
		);
		args.Handled = true;
	}
	}
}
