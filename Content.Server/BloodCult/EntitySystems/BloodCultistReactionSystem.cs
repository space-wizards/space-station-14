// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking.Rules;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Server.Popups;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// Unified system to handle all blood cultist reagent reactions.
/// This includes:
/// - Blood consumption (ingestion) -> causes bleeding, restores blood levels, adds to ritual pool
/// - Unholy Blood (touch) -> heals holy damage
/// - Holy Water (touch) -> deals additional holy damage
/// </summary>
public sealed class BloodCultistReactionSystem : EntitySystem
{
	[Dependency] private readonly BloodstreamSystem _bloodstream = default!;
	[Dependency] private readonly DamageableSystem _damageable = default!;
	[Dependency] private readonly PopupSystem _popup = default!;
	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly StaminaSystem _stamina = default!;

	public override void Initialize()
	{
		base.Initialize();

		// Single subscription to handle all cultist reagent reactions
		SubscribeLocalEvent<BloodCultistComponent, ReactionEntityEvent>(OnCultistReaction);
	}

	private void OnCultistReaction(EntityUid uid, BloodCultistComponent component, ref ReactionEntityEvent args)
	{
		// Handle blood ingestion
		if (args.Method == ReactionMethod.Ingestion)
		{
			HandleBloodIngestion(uid, ref args);
			return;
		}

		// Handle touch reactions
		if (args.Method == ReactionMethod.Touch)
		{
			// Check for Unholy Blood healing
			// This ONLY heals holy damage. Nothing else.
			if (args.Reagent.ID == "UnholyBlood")
			{
				HandleUnholyBloodTouch(uid, ref args);
			}
			// Check for Holy Water damage
			else if (args.Reagent.ID == "Holywater")
			{
				HandleHolyWaterTouch(uid, ref args);
			}
		}
	}

	/// <summary>
	/// Handles blood consumption by cultists.
	/// Used to be used to collect blood for the ritual pool, but that's been removed.
	/// </summary>
	private void HandleBloodIngestion(EntityUid uid, ref ReactionEntityEvent args)
	{
		// Only process sacrifice blood reagents (not Unholy Blood)
		if (!BloodCultConstants.SacrificeBloodReagents.Contains(args.Reagent.ID))
			return;

		var bloodAmount = args.ReagentQuantity.Quantity;
		if (bloodAmount <= 0)
			return;

		// Restore the cultist's blood levels (like Saline)
		// Each unit of consumed blood restores 2 units of their blood volume
		if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
		{
			_bloodstream.TryModifyBloodLevel((uid, bloodstream), FixedPoint2.New(bloodAmount.Float() * 2.0f));
			
			/// Commented out below code. Why did I ever think it was a good idea to bleed when you drink blood?
			// Cause brief bleeding (1 unit/second for each 5 units consumed)
			// This represents the blood being processed through their system
			//var bleedAmount = bloodAmount.Float() / 5.0f;
			//if (bleedAmount > 0.5f)
			//{
			//	_bloodstream.TryModifyBleedAmount(uid, bleedAmount, bloodstream);
			//}
			
		}

		/// Commented out, handled by the blooddrinker flag now
		// Heal a very tiny amount of toxin damage (0.5 toxin per 10u blood)
		// This is to make sure blood cultists don't have to worry too much about drinking from the floor.
		//if (TryComp<DamageableComponent>(uid, out var damageable))
		//{
		//	if (damageable.Damage.DamageDict.TryGetValue("Poison", out var toxinDamage) && toxinDamage > 0)
		//	{
		//		var healAmount = bloodAmount.Float() * 0.05f;  // Very tiny: 0.5 per 10u
		//		if (healAmount > 0)
		//		{
		//			var healSpec = new DamageSpecifier();
		//			healSpec.DamageDict.Add("Poison", FixedPoint2.New(-healAmount));
		//			_damageable.TryChangeDamage(uid, healSpec, false, false, damageable);
		//		}
		//	}
		//}

		// Visual feedback
		_popup.PopupEntity(
			Loc.GetString("cult-blood-consumed", ("amount", bloodAmount.Float())),
			uid, uid, PopupType.Small
		);
	}

	/// <summary>
	/// Handles Unholy Blood touch reactions for blood cultists.
	/// When a cultist touches Unholy Blood, it heals their holy damage.
	/// </summary>
	private void HandleUnholyBloodTouch(EntityUid uid, ref ReactionEntityEvent args)
	{
		// Check if the cultist has any holy damage
		if (!TryComp<DamageableComponent>(uid, out var damageable))
			return;

		// Get the amount of holy damage
		if (!damageable.Damage.DamageDict.TryGetValue("Holy", out var holyDamage) || holyDamage <= 0)
			return;

		// Calculate healing amount based on Unholy Blood quantity
		// 5u of Unholy Blood heals 1 point of holy damage
		var healAmount = args.ReagentQuantity.Quantity.Float() / 5.0f;
		if (healAmount <= 0)
			return;

		// Don't heal more than the actual holy damage
		healAmount = Math.Min(healAmount, holyDamage.Float());

		// Heal the holy damage
		var healSpec = new DamageSpecifier();
		healSpec.DamageDict.Add("Holy", FixedPoint2.New(-healAmount));
		_damageable.TryChangeDamage((uid, damageable), healSpec, false, false);

		// Visual and audio feedback
		_popup.PopupEntity(
			Loc.GetString("cult-unholy-blood-heal", ("amount", Math.Round(healAmount, 1))),
			uid, uid, PopupType.Medium
		);

		_audio.PlayPvs(
			new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"),
			Transform(uid).Coordinates
		);
	}

	/// <summary>
	/// Handles HolyWater touch reactions for blood cultists.
	/// When a cultist is splashed with holy water, they take additional holy damage and gain deconversion progress.
	/// </summary>
	private void HandleHolyWaterTouch(EntityUid uid, ref ReactionEntityEvent args)
	{
		// Calculate damage amount based on HolyWater quantity
		// The reagent already does 0.5 Holy damage per unit via its reactiveEffects
		// We add an additional 1.0 Holy damage per unit for cultists (total: 1.5 per unit)
		var damageAmount = args.ReagentQuantity.Quantity.Float() * 1.0f;
		if (damageAmount <= 0)
			return;
		else
		{
			// Apply additional holy damage to the cultist
			// This doesn't currently actually do anything as holy damage is not currently working
			if (TryComp<DamageableComponent>(uid, out var damageableForHoly))
			{
				var damageHoly = new DamageSpecifier();
				damageHoly.DamageDict.Add("Holy", FixedPoint2.New(damageAmount));
				_damageable.TryChangeDamage((uid, damageableForHoly), damageHoly, false, true);

				// Apply additional heat damage to the cultist
				// It only does 1/10th of the holy damage, because it shouldn't crit them from holy damage, it should just make them burned.
				var damageHeat = new DamageSpecifier();
				damageHeat.DamageDict.Add("Heat", FixedPoint2.New(damageAmount/10));
				_damageable.TryChangeDamage((uid, damageableForHoly), damageHeat, false, true);
			}

			// Visual and audio feedback
			_popup.PopupEntity(
				Loc.GetString("cult-holywater-burn", ("amount", Math.Round(damageAmount + args.ReagentQuantity.Quantity.Float() * 0.5f, 1))),
				uid, uid, PopupType.LargeCaution
			);

			_audio.PlayPvs(
				new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"),
				Transform(uid).Coordinates
			);
		}

		if (!TryComp<BloodCultistComponent>(uid, out var bloodCultist))
			return;

		// Make it so that the deCultify effect is only 25% as strong as the metabolism version, because touch effects multiply how much is being sprayed
		// The idea is to make it so you can deconvert with a fire extinguisher, but that a cultist would probably reasonably kill you first in a fair fight.
		var deCultifyMultiplier = .35f; 
		
		var scale = args.ReagentQuantity.Quantity.Float();
		var deCultifyAmount = scale * deCultifyMultiplier;

		var oldDeCultification = bloodCultist.DeCultification;
		var newDeCultification = oldDeCultification + deCultifyAmount;
		bloodCultist.DeCultification = newDeCultification;

		// If this application causes deconversion (crosses 100 threshold), play sound and knock down
		if (oldDeCultification < 100.0f && newDeCultification >= 100.0f)
		{
			// Play holy sound
			_audio.PlayPvs(
				new SoundPathSpecifier("/Audio/Effects/holy.ogg"),
				uid,
				AudioParams.Default
			);

			// Apply stamina damage to knock them down
			_stamina.TakeStaminaDamage(uid, 100f, visual: false);
		}
	}
}
