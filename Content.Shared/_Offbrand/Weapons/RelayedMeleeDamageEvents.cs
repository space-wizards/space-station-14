/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Offbrand.Weapons;

[ByRefEvent]
public record struct RelayedGetMeleeDamageEvent(GetMeleeDamageEvent Args);

[ByRefEvent]
public record struct RelayedGetMeleeAttackRateEvent(GetMeleeAttackRateEvent Args);
