// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Marks a rune as part of the final summoning ritual.
/// Three of these runes surround the rift and must be activated simultaneously.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FinalSummoningRuneComponent : Component
{
	/// <summary>
	/// The rift this rune is associated with.
	/// </summary>
	[DataField]
	public EntityUid? RiftUid = null;
}

