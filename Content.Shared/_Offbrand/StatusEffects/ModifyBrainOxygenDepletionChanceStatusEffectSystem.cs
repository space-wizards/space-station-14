/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class ModifyBrainOxygenDepletionChanceStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly HeartSystem _heart = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyBrainOxygenDepletionChanceStatusEffectComponent, StatusEffectRelayedEvent<BeforeDepleteBrainOxygen>>(OnBeforeDepleteBrainOxygen);
    }

    private void OnBeforeDepleteBrainOxygen(Entity<ModifyBrainOxygenDepletionChanceStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeDepleteBrainOxygen> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        var oxygenation = _heart.BloodOxygenation((target, Comp<HeartrateComponent>(target)));
        if (ent.Comp.OxygenationModifierThresholds.LowestMatch(oxygenation) is not { } modifier)
            return;

        args.Args = args.Args with { Chance = args.Args.Chance * modifier };
    }
}
