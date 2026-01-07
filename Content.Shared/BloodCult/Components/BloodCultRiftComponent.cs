// SPDX-FileCopyrightText: 2026 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System;
using System.Collections.Generic;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Marks an entity as a Blood Cult reality rift that pulses Unholy Blood.
/// The final ritual requires cultists to chant on runes around this rift.
/// This is a server-only component that tracks ritual state.
/// </summary>
[RegisterComponent]
public sealed partial class BloodCultRiftComponent : Component
{
	/// <summary>
	/// Time between pulses (in seconds).
	/// </summary>
	[DataField]
	public float PulseInterval = 30f;

	/// <summary>
	/// Time until next pulse.
	/// </summary>
	[DataField]
	public float TimeUntilNextPulse = 30f;

	/// <summary>
	/// Amount of Unholy Blood to add per pulse.
	/// </summary>
	[DataField]
	public float BloodPerPulse = 50f;

	/// <summary>
	/// The 3 summoning runes associated with this rift.
	/// </summary>
	[DataField]
	public List<EntityUid> SummoningRunes = new();

	/// <summary>
	/// Offering runes positioned around the anomaly for sacrifices.
	/// </summary>
	[DataField]
	public List<EntityUid> OfferingRunes = new();

	/// <summary>
	/// How often to refresh rune tracking.
	/// </summary>
	[DataField] public float RuneRefreshInterval = 0.5f;

	/// <summary>
	/// Time remaining until next rune refresh.
	/// </summary>
	[DataField] public float TimeUntilRuneRefresh = 0f;

	/// <summary>
	/// Is the final ritual currently in progress?
	/// </summary>
	[DataField]
	public bool RitualInProgress = false;

	/// <summary>
	/// Sequence of shakes remaining to play during the final ritual.
	/// </summary>
	[DataField]
	public List<float> ShakeDelays = new();

	/// <summary>
	/// Index into <see cref="ShakeDelays"/> for the next shake.
	/// </summary>
	[DataField]
	public int NextShakeIndex = 0;

	/// <summary>
	/// Time remaining until the next scheduled shake.
	/// </summary>
	[DataField]
	public float TimeUntilNextShake = 0f;

	/// <summary>
	/// Whether the final sacrifice event should trigger after the shake sequence completes.
	/// </summary>
	[DataField]
	public bool FinalSacrificePending = false;

	/// <summary>
	/// Whether the final sacrifice has already occurred.
	/// </summary>
	[DataField]
	public bool FinalSacrificeDone = false;


	/// <summary>
	/// Current chant step in the final ritual.
	/// </summary>
	[DataField]
	public int CurrentChantStep = 0;

	/// <summary>
	/// Total chant steps needed.
	/// </summary>
	[DataField]
	public int RequiredSacrifices = 3;

	/// <summary>
	/// Number of sacrifices completed during the final ritual.
	/// </summary>
	[DataField]
	public int SacrificesCompleted = 0;

	/// <summary>
	/// Total chant steps needed.
	/// </summary>
	[DataField]
	public int TotalChantSteps = 18;

	/// <summary>
	/// Time between chants (in seconds).
	/// </summary>
	[DataField]
	public float ChantInterval = 12f;

	/// <summary>
	/// Time until next chant.
	/// </summary>
	[DataField]
	public float TimeUntilNextChant = 0f;

	/// <summary>
	/// When the "not enough cultists" popup was last shown, to prevent spam.
	/// </summary>
	[DataField]
	public TimeSpan LastNotEnoughCultistsPopup = TimeSpan.Zero;

	/// <summary>
	/// The cultist earmarked for the next sacrifice cycle.
	/// This tracks who leads the chant.
	/// </summary>
	[DataField]
	public EntityUid? PendingSacrifice = null;

	/// <summary>
	/// Chants completed in the current sacrifice cycle.
	/// Once 3 chants are completed, Nar'Sie is summoned.
	/// </summary>
	[DataField]
	public int ChantsCompletedInCycle = 0;

	/// <summary>
	/// Music played during the final ritual.
	/// </summary>
	[DataField]
	public SoundSpecifier RitualMusic = new SoundCollectionSpecifier("BloodCultRitualMusic");

	/// <summary>
	/// Tracks whether the ritual music is currently active.
	/// </summary>
	[DataField]
	public bool RitualMusicPlaying = false;

	/// <summary>
	/// Minimum number of chanting cultists required to sustain the ritual.
	/// This number is reduced by 1 every time a cultist is killed during the ritual.
	/// </summary>
	[DataField]
	public int RequiredCultistsForChant = 3;

	/// <summary>
	/// Time when the ritual started, used to calculate elapsed time for music-synced sacrifices.
	/// </summary>
	[DataField]
	public TimeSpan RitualStartTime = TimeSpan.Zero;

	/// <summary>
	/// Time until the next "Nar'Sie" chant from non-sacrifice cultists (every 3 seconds).
	/// </summary>
	[DataField]
	public float TimeUntilNextNarsieChant = 0f;

	/// <summary>
	/// Time until the next shake effect, used to sync longer phrase chanting.
	/// </summary>
	[DataField]
	public float TimeUntilNextShakeForChant = 0f;

	/// <summary>
	/// Duration of the ritual music in seconds, calculated when ritual starts.
	/// </summary>
	[DataField]
	public float RitualMusicDuration = 0f;

	/// <summary>
	/// Time when the last sacrifice was performed.
	/// Used to enforce a minimum 5-second cooldown between sacrifices.
	/// </summary>
	[DataField]
	public TimeSpan TimeSinceLastSacrifice = TimeSpan.Zero;
}

