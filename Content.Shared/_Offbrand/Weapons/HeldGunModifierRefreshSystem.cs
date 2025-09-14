/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Hands;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Offbrand.Weapons;

public sealed partial class HeldGunModifierRefreshSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
    }

    private void OnGotEquippedHand(Entity<GunComponent> ent, ref GotEquippedHandEvent args)
    {
        _gun.RefreshModifiers(ent.AsNullable());
    }

    private void OnGotUnequippedHand(Entity<GunComponent> ent, ref GotUnequippedHandEvent args)
    {
        _gun.RefreshModifiers(ent.AsNullable());
    }
}

[ByRefEvent]
public record struct RelayedGunRefreshModifiersEvent(GunRefreshModifiersEvent Args);
