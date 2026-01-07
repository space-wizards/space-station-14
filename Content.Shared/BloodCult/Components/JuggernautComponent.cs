// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Angery fella.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JuggernautComponent : Component
{
	/// <summary>
	/// The soulstone that was used to create this juggernaut.
	/// Will be ejected when the juggernaut becomes critical.
	/// </summary>
	[DataField]
	public EntityUid? SourceSoulstone;

	/// <summary>
	/// The dead body that was used to create this juggernaut.
	/// Will be ejected when the juggernaut becomes critical or dies.
	/// </summary>
	[DataField]
	public EntityUid? SourceBody;

	/// <summary>
	/// Whether the juggernaut is currently inactive (soulstone/body has been ejected).
	/// Inactive juggernauts cannot move or act, even if healed, until a soulstone or body is reinserted.
	/// </summary>
	[DataField, AutoNetworkedField]
	public bool IsInactive;

	/// <summary>
	/// Message stored for commune
	/// </summary>
	[DataField]
	public string? CommuningMessage = null;
}
