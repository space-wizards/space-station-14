// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
///     Raised directed on the gun entity when ammo is shot to calculate its spread.
/// </summary>
/// <param name="Spread">The spread of the ammo, can be changed by handlers.</param>
[ByRefEvent]
public record struct GunGetAmmoSpreadEvent(Angle Spread);
