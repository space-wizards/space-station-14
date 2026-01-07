// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Offer a non-cultist if Triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OfferOnTriggerComponent : Component
{
	/// <summary>
    ///     The range at which the offer rune can function.
    /// </summary>
    [DataField] public float OfferRange = 0.2f;

	/// <summary>
	///	    The range at which cultists can contribute to an invocation.
	/// </summary>
	[DataField] public float InvokeRange = 1.4f;
}
