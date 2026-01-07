// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Server.Hands.Systems;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Server.BloodCult.Components;

namespace Content.Server.BloodCult.EntitySystems;

public sealed class BloodCultMeleeWeaponSystem : EntitySystem
{
	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly PopupSystem _popupSystem = default!;
	[Dependency] private readonly HandsSystem _hands = default!;
	[Dependency] private readonly SharedAudioSystem _audioSystem = default!;

	public override void Initialize()
    {
        base.Initialize();

		SubscribeLocalEvent<BloodCultMeleeWeaponComponent, MeleeHitEvent>(OnBloodCultMeleeHit);
	}

	private void OnBloodCultMeleeHit(EntityUid uid, BloodCultMeleeWeaponComponent comp, MeleeHitEvent args)
	{
		bool blockedByChaplain = false;
		bool blockedByCultist = false;

		if (args.IsHit &&
			args.HitEntities.Any())
		{
			// Cast to List to modify - MeleeHitEvent is constructed with a List<EntityUid>
			if (args.HitEntities is not List<EntityUid> hitList)
				return;
			
			// Remove protected entities from the hit list instead of canceling the entire event
			// This allows hitting enemies while protecting allies in wide attacks
			for (int i = hitList.Count - 1; i >= 0; i--)
			{
				var target = hitList[i];
				
				if (HasComp<CultResistantComponent>(target))
				{
					blockedByChaplain = true;
					hitList.RemoveAt(i);
				}
				else if (HasComp<BloodCultistComponent>(target) || HasComp<BloodCultConstructComponent>(target))
				{
					blockedByCultist = true;
					hitList.RemoveAt(i);
				}
			}
			
			// Only cancel the entire event if ALL entities were protected (no valid targets remain)
			if (hitList.Count == 0)
				args.Handled = true;
		}

		if (blockedByChaplain)
		{
		_popupSystem.PopupEntity(
				Loc.GetString("cult-attack-repelled"),
				args.User, args.User, PopupType.MediumCaution
			);
		var coordinates = Transform(args.User).Coordinates;
		_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Effects/holy.ogg"), coordinates);
		var offsetRandomCoordinates = coordinates.Offset(_random.NextVector2(1f, 1.5f));
            _hands.ThrowHeldItem(args.User, offsetRandomCoordinates);
		}
		if (blockedByCultist)
		{
			_popupSystem.PopupEntity(
					Loc.GetString("cult-attack-teamhit"),
					args.User, args.User, PopupType.MediumCaution
				);
		}
	}
}
