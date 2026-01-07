// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Component that marks a blood cult rune as cleanable with soap, spray bottles, or cleaning grenades.
/// Only basic runes (not tear veil or final rift runes) should have this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CleanableRuneComponent : Component
{
}

