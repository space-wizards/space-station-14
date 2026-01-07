// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later or MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Summon structures when triggered with the appropriate material stacks.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SummonOnTriggerComponent : Component
{
	/// <summary>
    ///     The range at which the summon rune can find material stacks.
    /// </summary>
    [DataField] public float SummonRange = 0.3f;
}
