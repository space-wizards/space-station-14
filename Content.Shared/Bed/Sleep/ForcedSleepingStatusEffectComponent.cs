// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep;

/// <summary>
/// Prevents waking up. Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class ForcedSleepingStatusEffectComponent : Component;
