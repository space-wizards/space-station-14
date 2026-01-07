// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Marks a rune as a Tear the Veil ritual rune.
/// Multiple cultists must stand on these runes to complete the ritual through chanting.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TearVeilComponent : Component
{
	/// <summary>
	/// Is this rune currently part of an active ritual?
	/// </summary>
	[DataField]
	public bool RitualInProgress = false;

	/// <summary>
	/// The map this ritual is taking place on.
	/// </summary>
	[DataField]
	public EntityUid? RitualMapUid = null;

	/// <summary>
	/// Current chant step in the ritual (0-based).
	/// </summary>
	[DataField]
	public int CurrentChantStep = 0;

	/// <summary>
	/// Total number of chant steps required to complete the ritual.
	/// </summary>
	[DataField]
	public int TotalChantSteps = 6;

	/// <summary>
	/// Time between each chant step (in seconds).
	/// </summary>
	[DataField]
	public float ChantInterval = 5f;

	/// <summary>
	/// Time until the next chant step.
	/// </summary>
	[DataField]
	public float TimeUntilNextChant = 0f;

	/// <summary>
	/// Minimum number of cultists required on runes to complete the ritual.
	/// </summary>
	[DataField]
	public int MinimumCultists = 3;

	/// <summary>
	/// Has this rune already been used to successfully weaken the veil?
	/// </summary>
	[DataField]
	public bool RitualCompleted;
}
