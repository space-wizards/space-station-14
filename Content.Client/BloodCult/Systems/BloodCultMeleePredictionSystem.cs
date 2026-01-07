// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Collections.Generic;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;

namespace Content.Client.BloodCult.Systems;

/// <summary>
/// Mirrors the server-side ally protection logic so clientside prediction doesn't show fake hits.
/// </summary>
public sealed class BloodCultMeleePredictionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodCultMeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, BloodCultMeleeWeaponComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (args.HitEntities is not List<EntityUid> hitList)
            return;

        var removedAny = false;

        for (var i = hitList.Count - 1; i >= 0; i--)
        {
            var target = hitList[i];

            if (!HasComp<BloodCultistComponent>(target) && !HasComp<BloodCultConstructComponent>(target))
                continue;

            hitList.RemoveAt(i);
            removedAny = true;
        }

        if (!removedAny)
            return;

        if (hitList.Count == 0)
            args.Handled = true;
    }
}

