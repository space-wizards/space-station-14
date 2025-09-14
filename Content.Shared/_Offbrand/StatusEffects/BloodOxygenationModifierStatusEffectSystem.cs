/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class BloodOxygenationModifierStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodOxygenationModifierStatusEffectComponent, StatusEffectRelayedEvent<GetOxygenationModifier>>(OnGetOxygenationModifier);
    }

    private void OnGetOxygenationModifier(Entity<BloodOxygenationModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetOxygenationModifier> args)
    {
        var theirs = args.Args.Modifier.Double();
        var ours = ent.Comp.MinimumOxygenation.Double();

        args.Args = args.Args with { Modifier = theirs + ours - (theirs * ours) };
    }
}
