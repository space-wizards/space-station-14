/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class BrainHealingStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainHealingStatusEffectComponent, StatusEffectRelayedEvent<BeforeHealBrainDamage>>(OnBeforeHealBrainDamage);
    }

    private void OnBeforeHealBrainDamage(Entity<BrainHealingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeHealBrainDamage> args)
    {
        args.Args = args.Args with { Heal = true };
    }
}
