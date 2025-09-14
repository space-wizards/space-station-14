/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared._Offbrand.Chemistry;
using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Zombies;

namespace Content.Shared._Offbrand.Zombies;

public sealed class InjectionResistanceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, BeforeInjectOnEventEvent>(OnBeforeInjectOnEvent);
    }

    private void OnBeforeInjectOnEvent(Entity<BloodstreamComponent> ent, ref BeforeInjectOnEventEvent args)
    {
        var evt = new ZombificationResistanceQueryEvent(SlotFlags.WITHOUT_POCKET);
        RaiseLocalEvent(ent, evt);

        args.InjectionAmount *= evt.TotalCoefficient;
    }
}
