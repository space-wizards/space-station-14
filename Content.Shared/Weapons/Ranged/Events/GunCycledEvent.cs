// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised directed on a gun when it cycles.
/// </summary>
[ByRefEvent]
public readonly record struct GunCycledEvent;
