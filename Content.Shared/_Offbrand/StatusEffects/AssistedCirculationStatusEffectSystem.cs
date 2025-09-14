/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared._Offbrand.Wounds;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed partial class AssistedCirculationStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AssistedCirculationStatusEffectComponent, StatusEffectRelayedEvent<GetStoppedCirculationModifier>>(OnGetStoppedCirculationModifier);
    }

    private void OnGetStoppedCirculationModifier(Entity<AssistedCirculationStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetStoppedCirculationModifier> args)
    {
        args.Args = args.Args with { Modifier = FixedPoint2.Clamp(args.Args.Modifier + ent.Comp.Amount, 0, 1) };
    }
}
