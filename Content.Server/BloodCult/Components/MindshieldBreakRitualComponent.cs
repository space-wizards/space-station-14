// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.Map;

namespace Content.Server.BloodCult.Components;

/// <summary>
/// Tracks active mindshield breaking ritual state for periodic chanting
/// </summary>
[RegisterComponent]
public sealed partial class MindshieldBreakRitualComponent : Component
{
	/// <summary>
	/// The victim being targeted by the ritual
	/// </summary>
	[DataField]
	public EntityUid Victim;

	/// <summary>
	/// The cultists participating in the ritual
	/// </summary>
	[DataField]
	public EntityUid[] Participants = Array.Empty<EntityUid>();

	/// <summary>
	/// The location of the offering rune
	/// </summary>
	[DataField]
	public EntityCoordinates RuneLocation;

	/// <summary>
	/// The offering rune entity
	/// </summary>
	[DataField]
	public EntityUid RuneEntity;

	/// <summary>
	/// When the ritual started
	/// </summary>
	[DataField]
	public TimeSpan StartTime;

	/// <summary>
	/// Next time the cultists will chant (every 2 seconds)
	/// </summary>
	[DataField]
	public TimeSpan NextChantTime;

	/// <summary>
	/// How many times the cultists have chanted (0-3)
	/// </summary>
	[DataField]
	public int ChantCount = 0;
}

