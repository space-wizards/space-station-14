using System.Collections.Generic;
using Content.Shared.BloodCult.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.BloodCult;

/// <summary>
/// Filters cult melee hit list: removes allies (cultists) and chaplains (CultResistant) so they take no damage.
/// Raises events for the server to react with popups/sound/throw.
/// </summary>
public sealed class BloodCultMeleeWeaponSystem : EntitySystem
{
	public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<BloodCultMeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
	}

	private void OnMeleeHit(Entity<BloodCultMeleeWeaponComponent> ent, ref MeleeHitEvent args)
	{
		if (!args.IsHit || args.HitEntities.Count == 0)
			return;

		if (args.HitEntities is not List<EntityUid> hitList)
			return;

		var blockedByChaplain = false;
		var blockedByCultist = false;

		for (var i = hitList.Count - 1; i >= 0; i--)
		{
			var target = hitList[i];

			if (HasComp<CultResistantComponent>(target))
			{
				blockedByChaplain = true;
				hitList.RemoveAt(i);
			}
			// When BloodCultConstructComponent exists, add: || HasComp<BloodCultConstructComponent>(target)
			else if (HasComp<BloodCultistComponent>(target))
			{
				blockedByCultist = true;
				hitList.RemoveAt(i);
			}
		}

		if (hitList.Count == 0)
			args.Handled = true;

		if (blockedByCultist)
		{
			var ev = new BloodCultMeleeAllyBlockedAttemptEvent(args.User, args.Weapon);
			RaiseLocalEvent(args.Weapon, ref ev);
		}

		if (blockedByChaplain)
		{
			var ev = new BloodCultMeleeChaplainBlockedAttemptEvent(args.User, args.Weapon);
			RaiseLocalEvent(args.Weapon, ref ev);
		}
	}
}
