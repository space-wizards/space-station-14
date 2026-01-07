// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Revive a cultist if Triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReviveCultistOnTriggerComponent : Component
{
	/// <summary>
    ///     The range at which the revive rune can detect dead targets.
    /// </summary>
    [DataField] public float ReviveRange = 0.8f;
}
