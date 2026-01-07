// SPDX-FileCopyrightText: 2026 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server.Audio;
using Content.Server.Camera;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Body.Systems;
using Content.Server.Mind;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.Audio;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Trigger;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Mind.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Maths;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// Handles Blood Cult rift pulsing (adding Unholy Blood) and final summoning ritual.
/// </summary>
public sealed partial class BloodCultRiftSystem : EntitySystem
{
	private const float ShakeRange = 25f;
	private const float SummoningRuneDetectionRange = 1.5f;
	private const float FinalRitualShakeIntensity = 9f;
	private static readonly float[] SacrificeChantDelays = { 15f, 10f, 7f, 5f, 3f, 1f };

	[Dependency] private readonly PopupSystem _popupSystem = default!;
	[Dependency] private readonly MobStateSystem _mobState = default!;
	[Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;
	[Dependency] private readonly EntityLookupSystem _lookup = default!;
	[Dependency] private readonly AppearanceSystem _appearance = default!;
	[Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
	[Dependency] private readonly CameraRecoilSystem _cameraRecoil = default!;
	[Dependency] private readonly SharedTransformSystem _transformSystem = default!;
	[Dependency] private readonly IPlayerManager _playerManager = default!;
	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly ExplosionSystem _explosionSystem = default!;
	[Dependency] private readonly BodySystem _bodySystem = default!;
	//[Dependency] private readonly MindSystem _mindSystem = default!;
	[Dependency] private readonly OfferOnTriggerSystem _offerSystem = default!;
	[Dependency] private readonly IGameTiming _timing = default!;
	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
	[Dependency] private readonly ChatSystem _chatSystem = default!;
	[Dependency] private readonly PuddleSystem _puddleSystem = default!;

	public override void Initialize()
	{
		base.Initialize();

		// Run before OfferOnTriggerSystem so ritual check happens first
		SubscribeLocalEvent<FinalSummoningRuneComponent, TriggerEvent>(OnTriggerRitual, before: new[] { typeof(OfferOnTriggerSystem) });
		SubscribeLocalEvent<BloodCultRiftComponent, TriggerEvent>(OnTriggerRitualFromRift, before: new[] { typeof(OfferOnTriggerSystem) });
	}

	public override void Update(float frameTime)
	{
		base.Update(frameTime);

		// Process all rifts
		// All uh... one of them. If there's more than one there's a bug. But hey, it could happen? Hopefully not.
		// But the EntityQueryEnumerator would break if we only had it handle one, so it works.
		var riftQuery = EntityQueryEnumerator<BloodCultRiftComponent, TransformComponent>();
		while (riftQuery.MoveNext(out var riftUid, out var riftComp, out var xform))
		{
			// Track if we've already processed a sacrifice this frame to prevent multiple sacrifices
			var sacrificeProcessedThisFrame = false;
			// Handle pulsing (adding Unholy Blood)
			riftComp.TimeUntilNextPulse -= frameTime;
			if (riftComp.TimeUntilNextPulse <= 0)
			{
				PulseRift(riftUid, riftComp);
				riftComp.TimeUntilNextPulse = riftComp.PulseInterval;
			}

			// No longer needed - FinalRiftRune is permanent and always in range

			// Handle active ritual chanting
			if (riftComp.RitualInProgress)
			{
				// Check if enough living cultists are present
				// GetCultistsOnSummoningRunes filters out soulstones, juggernauts, shades, ghosts, dead, and critical entities
				var cultistsOnRunes = GetCultistsOnSummoningRunes(riftComp);
				
				// RequiredCultistsForChant decreases after each sacrifice (3 -> 2 -> 1)
				// This allows the ritual to continue with fewer cultists as it progresses
				if (cultistsOnRunes.Count < riftComp.RequiredCultistsForChant)
				{
					// Not enough living cultists, pause the ritual (don't fail)
					// Stop music and pause timers, but keep RitualInProgress true so it can resume
					if (riftComp.RitualMusicPlaying)
					{
						_sound.StopStationEventMusic(riftUid, StationEventMusicType.BloodCult);
						riftComp.RitualMusicPlaying = false;
					}
					
					// Clear pending sacrifice if it's no longer valid (e.g., became soulstone, juggernaut, shade, died, or left)
					if (riftComp.PendingSacrifice is { } pending)
					{
						if (!cultistsOnRunes.Contains(pending) || !IsValidSummoningParticipant(pending))
						{
							riftComp.PendingSacrifice = null;
						}
					}
					// The ritual will automatically resume if or when enough living cultists return (next frame)
					continue;
				}

				// Ensure we have a pending sacrifice
				EnsurePendingSacrifice(riftComp, cultistsOnRunes);

				// If we were paused (music stopped) and now have enough cultists, resume the music
				if (!riftComp.RitualMusicPlaying && riftComp.RitualMusicDuration > 0f)
				{
					// Restart music at the correct offset based on sacrifices completed
					// This handles the case where ritual was paused due to not enough cultists
					var resolved = _audio.ResolveSound(riftComp.RitualMusic);
					if (!ResolvedSoundSpecifier.IsNullOrEmpty(resolved))
					{
						// Calculate playback offset based on sacrifice progress
						float offset = 0f;
						if (riftComp.RequiredSacrifices > 0 && riftComp.SacrificesCompleted > 0)
						{
							var progress = (float)riftComp.SacrificesCompleted / riftComp.RequiredSacrifices;
							offset = riftComp.RitualMusicDuration * progress;
							offset = Math.Clamp(offset, 0f, riftComp.RitualMusicDuration);
						}

						// Reset ritual start time to now (so elapsed time calculation works correctly)
						// The music offset ensures we're at the right point in the track
						riftComp.RitualStartTime = _timing.CurTime;

						// Note: DispatchStationEventMusic doesn't accept AudioParams directly
						// The offset is handled by the music track itself
						_sound.DispatchStationEventMusic(riftUid, resolved, StationEventMusicType.BloodCult);
						riftComp.RitualMusicPlaying = true;
					}
				}

				// Handle music-synced sacrifices
				// Only process one sacrifice per frame to prevent all three from triggering simultaneously
				if (riftComp.RitualMusicDuration > 0f && riftComp.RitualStartTime != TimeSpan.Zero && !sacrificeProcessedThisFrame)
				{
					// Only check for the NEXT sacrifice, not all of them
					// This prevents multiple sacrifices from triggering in the same frame
					if (riftComp.SacrificesCompleted < riftComp.RequiredSacrifices)
					{
						// Store the current sacrifice index to prevent race conditions
						// If SacrificesCompleted changes during this check, we'll catch it
						var currentSacrificeIndex = riftComp.SacrificesCompleted;
						
						// Elapsed time since RitualStartTime was set
						var elapsed = (_timing.CurTime - riftComp.RitualStartTime).TotalSeconds;
						
						// Calculate the actual position in the music track
						// If music was resumed, RitualStartTime was reset and music plays from an offset
						var currentMusicPosition = elapsed;
						if (riftComp.RequiredSacrifices > 0 && currentSacrificeIndex > 0)
						{
							// Expected minimum position if playing continuously
							var minExpectedPosition = (riftComp.RitualMusicDuration / riftComp.RequiredSacrifices) * currentSacrificeIndex;
							
							// If elapsed is less than the minimum expected, music was resumed
							// Add the offset to get the actual position in the track
							if (elapsed < minExpectedPosition - 1.0f) // 1 second tolerance for timing variations
							{
								var progress = (float)currentSacrificeIndex / riftComp.RequiredSacrifices;
								var musicOffset = riftComp.RitualMusicDuration * progress;
								currentMusicPosition = musicOffset + elapsed;
							}
						}
						
						// Validate that elapsed time is reasonable (not negative or impossibly large)
						// This prevents issues if RitualStartTime is set incorrectly
						if (elapsed < 0f || elapsed > riftComp.RitualMusicDuration * 2f)
						{
							// Timing is invalid, skip this frame
							continue;
						}
						
						// Calculate target time for the NEXT sacrifice (currentSacrificeIndex + 1)
						// Each sacrifice should happen at: (duration / requiredSacrifices) * sacrificeNumber
						// For 3 sacrifices: at duration/3, 2*duration/3, and duration
						var targetMusicTime = (riftComp.RitualMusicDuration / riftComp.RequiredSacrifices) * (currentSacrificeIndex + 1);
						
						// CRITICAL SAFETY: Enforce minimum 5-second cooldown between sacrifices
						// This prevents multiple sacrifices from triggering in rapid succession
						var timeSinceLastSacrifice = (_timing.CurTime - riftComp.TimeSinceLastSacrifice).TotalSeconds;
						if (timeSinceLastSacrifice < 5.0f && riftComp.TimeSinceLastSacrifice != TimeSpan.Zero)
						{
							// Not enough time has passed since last sacrifice, skip
							continue;
						}
						
						// Check if it's time for the next sacrifice
						// Use a small tolerance (0.1s) to account for frame timing
						// Also check that we're not too far past the target time (max 0.5s tolerance)
						// This ensures we only trigger sacrifices at the right time, not way too early or late
						if (!sacrificeProcessedThisFrame && currentMusicPosition >= targetMusicTime - 0.1f && currentMusicPosition <= targetMusicTime + 0.5f)
						{
							// Double-check that SacrificesCompleted hasn't changed (prevent race condition)
							if (riftComp.SacrificesCompleted != currentSacrificeIndex)
							{
								// Another sacrifice happened this frame, skip
								continue;
							}
							
							// Re-check cultist count right before sacrifice to prevent sync issues
							var currentCultistsOnRunes = GetCultistsOnSummoningRunes(riftComp);
							if (currentCultistsOnRunes.Count >= riftComp.RequiredCultistsForChant)
							{
								// Mark that we're processing a sacrifice this frame
								// This prevents multiple sacrifices from being processed in the same frame
								sacrificeProcessedThisFrame = true;
								
								// Final validation: ensure SacrificesCompleted hasn't changed
								// This is a critical safety check to prevent duplicate sacrifices
								if (riftComp.SacrificesCompleted != currentSacrificeIndex)
								{
									// State changed, abort and reset flag
									sacrificeProcessedThisFrame = false;
									continue;
								}
								
								// Time for this sacrifice
								TryPerformFinalSacrifice(riftUid, riftComp, xform);
								
								// After a sacrifice, immediately check if ritual is complete
								// This prevents any further processing in this frame
								// CRITICAL: Check both SacrificesCompleted and FinalSacrificeDone to ensure summoning happens
								if (riftComp.SacrificesCompleted >= riftComp.RequiredSacrifices)
								{
									// Final sacrifice completed - summon Nar'Sie
									if (!riftComp.FinalSacrificeDone)
									{
										riftComp.FinalSacrificeDone = true;
									}
									
									// SUCCESS! Summon Nar'Sie
									SummonNarsie(riftUid, xform);
									AnnounceRitualSuccess();
									if (riftComp.RitualMusicPlaying)
									{
										_sound.StopStationEventMusic(riftUid, StationEventMusicType.BloodCult);
										riftComp.RitualMusicPlaying = false;
									}
									riftComp.RitualInProgress = false;
									riftComp.SacrificesCompleted = 0; // Reset on successful completion
									riftComp.TimeSinceLastSacrifice = TimeSpan.Zero; // Reset cooldown
									continue;
								}
							}
						}
					}
				}

				// Check if ritual is complete (all sacrifices done)
				// This is a fallback check
				if (riftComp.SacrificesCompleted >= riftComp.RequiredSacrifices)
				{
					// Final sacrifice completed - summon Nar'Sie
					if (!riftComp.FinalSacrificeDone)
					{
						riftComp.FinalSacrificeDone = true;
						// SUCCESS! Summon Nar'Sie
						SummonNarsie(riftUid, xform);
						AnnounceRitualSuccess();
						if (riftComp.RitualMusicPlaying)
						{
							_sound.StopStationEventMusic(riftUid, StationEventMusicType.BloodCult);
							riftComp.RitualMusicPlaying = false;
						}
						riftComp.RitualInProgress = false;
						riftComp.SacrificesCompleted = 0; // Reset on successful completion
						riftComp.TimeSinceLastSacrifice = TimeSpan.Zero; // Reset cooldown
						continue;
					}
				}

				// Handle "Nar'Sie" chants every 5 seconds (from non-sacrifice cultists)
				riftComp.TimeUntilNextNarsieChant -= frameTime;
				if (riftComp.TimeUntilNextNarsieChant <= 0f)
				{
					//Backup chanters
					DoNarsieChant(riftUid, riftComp, cultistsOnRunes);
					//Swapping to combine long shake with the chanting
					riftComp.TimeUntilNextNarsieChant = 5f;
					DoShakeWithLongChant(riftUid, riftComp, xform, cultistsOnRunes);
				}

				/* Commenting out separate shake and chant intervals, they should be synced
				// Handle shake-synced longer phrase chanting
				riftComp.TimeUntilNextShakeForChant -= frameTime;
				if (riftComp.TimeUntilNextShakeForChant <= 0f)
				{
					DoShakeWithLongChant(riftUid, riftComp, xform, cultistsOnRunes);
					// Schedule next shake - use a consistent interval (e.g., every 5 seconds)
					riftComp.TimeUntilNextShakeForChant = 5f;
				}
				*/
			}
		}
	}

	/// <summary>
	/// Adds Unholy Blood to the rift's solution and triggers visual/audio effects.
	/// </summary>
	private void PulseRift(EntityUid riftUid, BloodCultRiftComponent riftComp)
	{
		// If this check fails, something is very wrong. All rifts should have a solution container.
		if (!_solutionContainer.TryGetSolution(riftUid, "sanguine_pool", out var solutionEnt, out var solution))
			return;

		// Add Unholy Blood and spill any excess
		var amount = FixedPoint2.New(riftComp.BloodPerPulse);
		if (_solutionContainer.TryAddReagent(solutionEnt.Value, "UnholyBlood", amount, out var accepted))
		{
			// Using fancy adding of reagents to overflow so it actually acts like a bucket. Better than just spawning it on the floor directly.
			var overflow = amount - accepted;
			if (overflow > FixedPoint2.Zero)
				SpillOverflow(riftUid, overflow);
		}
		else
		{
			SpillOverflow(riftUid, amount);
		}

		// Trigger pulse animation
		// This is actually the normal liquid anom animation. Why not use existing animations.
		if (TryComp<AppearanceComponent>(riftUid, out var appearance))
		{
			_appearance.SetData(riftUid, AnomalyVisualLayers.Animated, true, appearance);
			// Animation will auto-hide after animation completes
		}
	}

	private void DoShake(EntityUid riftUid, TransformComponent xform, float intensity)
	{
		// Not sure how far out this shakes it for. But I think it works pretty good?
		// May have to adjust later. It works at close range pretty well.
		// todo: Test how much the shake actually works from across the map
		// todo: I'm pretty sure the shake direction is wrong. It should be from the rift to the player.
		var epicenter = _transformSystem.ToMapCoordinates(xform.Coordinates);
		var filter = Filter.Empty();
		filter.AddInRange(epicenter, ShakeRange, _playerManager, EntityManager);

		foreach (var session in filter.Recipients)
		{
			if (session.AttachedEntity is not EntityUid uid)
				continue;

			var playerPos = _transformSystem.GetWorldPosition(uid);
			var delta = epicenter.Position - playerPos;
			if (delta.LengthSquared() < 0.0001f)
				delta = new Vector2(0.01f, 0f);

			var distance = delta.Length();
			var effect = intensity * (1 - distance / ShakeRange);
			if (effect <= 0f)
				continue;

			_cameraRecoil.KickCamera(uid, -Vector2.Normalize(delta) * effect);
		}

		// Play the blood sound. I couldn't find a better sound for this.
		// todo: Find a better sound
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Fluids/blood1.ogg"), riftUid, AudioParams.Default.WithVolume(-3f));
	}

	private void TryPerformFinalSacrifice(EntityUid riftUid, BloodCultRiftComponent component, TransformComponent xform)
	{
		// Make sure it actually needs to be done.
		// This is a critical guard to prevent multiple sacrifices in the same frame
		if (component.FinalSacrificeDone || component.SacrificesCompleted >= component.RequiredSacrifices)
			return;

		// Re-check cultist count to prevent sync issues
		// This ensures we have enough cultists right before the sacrifice
		var cultistsOnRunes = GetCultistsOnSummoningRunes(component);
		
		// Ensure we still have enough cultists for the ritual
		if (cultistsOnRunes.Count < component.RequiredCultistsForChant)
		{
			return;
		}
			
		if (cultistsOnRunes.Count == 0)
		{
			return;
		}

		// If we haven't picked someone to lead the chant, pick someone. 
		// Whoever gets the fancy chant is the next to die. Hopefully that's spooky and ominous.
		// I want people thinking "Why am I saying something different from everyone else?"
		// Foreshadowing is fun *evil laugh*
		if (component.PendingSacrifice is not { } pending || !cultistsOnRunes.Contains(pending) || !IsValidSummoningParticipant(pending))
		{
			component.PendingSacrifice = cultistsOnRunes.Count > 0 ? _random.Pick(cultistsOnRunes) : null;
		}

		// If we don't have a valid pending sacrifice, abort
		if (component.PendingSacrifice is not { } validPending)
		{
			return;
		}

		// If the person who has the pending sacrifice flag doesn't exist, pick a new one.
		if (!TryComp(validPending, out TransformComponent? victimXform))
		{
			component.PendingSacrifice = cultistsOnRunes.Count > 0 ? _random.Pick(cultistsOnRunes) : null;
			return;
		}

		// If the person who has the pending sacrifice flag isn't on a rune or is no longer valid, pick a new one.
		if (!cultistsOnRunes.Contains(validPending) || !IsValidSummoningParticipant(validPending))
		{
			component.PendingSacrifice = cultistsOnRunes.Count > 0 ? _random.Pick(cultistsOnRunes) : null;
			return;
		}

		// Kill the sacrifice and soulstone them. If it can't kill them, skip this attempt.
		// This should never happen, because if the code gets this far it should be able to kill them.
		var victim = component.PendingSacrifice.Value;
		if (!_offerSystem.TryForceSoulstoneCreation(victim, victimXform.Coordinates))
		{
			return;
		}

		// Gib the body after the brain has been removed
		// Use the explode smite approach: queue an explosion and gib without organs
		// This prevents issues with organs that don't have ContainerManagerComponent
		if (Exists(victim))
		{
			var coords = _transformSystem.GetMapCoordinates(victim);
			_explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
				4, 1, 2, victim, maxTileBreak: 0);
			_bodySystem.GibBody(victim, gibOrgans: false);
		}

		//Increment the sacrifices, play an announcement.
		component.SacrificesCompleted++;
		// Decrease the required cultist count after each sacrifice
		// This allows the ritual to continue with fewer cultists as it progresses (3 -> 2 -> 1)
		// Minimum is 1 to ensure at least one cultist remains for the final sacrifice
		component.RequiredCultistsForChant = Math.Max(1, component.RequiredCultistsForChant - 1);
		component.PendingSacrifice = null;
		component.FinalSacrificePending = false;
		
		// Record the time of this sacrifice to enforce 5-second cooldown
		component.TimeSinceLastSacrifice = _timing.CurTime;

		AnnounceSacrificeProgress(component.SacrificesCompleted);

		// Note: Don't set FinalSacrificeDone here - let the Update loop handle it
		// This ensures the summoning logic in Update runs properly
		// The Update loop will check SacrificesCompleted and summon Nar'Sie
	}

	/// <summary>
	/// When a cultist clicks on the BloodCultRift itself, start the ritual.
	/// </summary>
	private void OnTriggerRitualFromRift(EntityUid uid, BloodCultRiftComponent component, TriggerEvent args)
	{
		// Only cultists can trigger the ritual
		if (args.User == null || !TryComp<BloodCultistComponent>(args.User, out var cultist))
		{
			args.Handled = true;
			return;
		}

		var user = args.User.Value;
		var riftUid = uid;

		// If ritual is in progress but paused (music not playing), allow resuming
		if (component.RitualInProgress && component.RitualMusicPlaying)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-already-in-progress"),
				user, user, PopupType.MediumCaution
			);
			args.Handled = true;
			return;
		}

		// If ritual is paused (in progress but music stopped), resume it
		if (component.RitualInProgress && !component.RitualMusicPlaying)
		{
			// Check if enough cultists are present to resume
			var cultistsOnRunesForResume = GetCultistsOnSummoningRunes(component);
			if (cultistsOnRunesForResume.Count < component.RequiredCultistsForChant)
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-final-ritual-not-enough-cultists",
						("current", cultistsOnRunesForResume.Count),
						("required", component.RequiredCultistsForChant)),
					user, user, PopupType.LargeCaution
				);
				args.Handled = true;
				return;
			}

			// Resume the ritual - music will be restarted in Update loop at correct offset
			args.Handled = true;
			return;
		}

		// Ensure the cult has weakened the veil (blood collected plus ritual)
		//Should never happen, since the rift spawns "after" the veil is weakened. But this covers weird use cases of admin-spawned rifts.
		if (!_bloodCultRule.TryGetActiveRule(out var ruleComp) || !ruleComp.VeilWeakened || ruleComp.BloodCollected < ruleComp.BloodRequiredForVeil)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-too-early",
					("collected", Math.Round(ruleComp.BloodCollected, 1)),
					("required", Math.Round(ruleComp.BloodRequiredForVeil, 1))),
				user, user, PopupType.LargeCaution
			);
			args.Handled = true;
			return;
		}

		component.RequiredCultistsForChant = 3;

		// Check if enough runes have cultists on them
		var cultistsOnRunes = GetCultistsOnSummoningRunes(component);
		if (cultistsOnRunes.Count < component.RequiredCultistsForChant)
		{
			var allowPopup = component.LastNotEnoughCultistsPopup == TimeSpan.Zero ||
				(_timing.CurTime - component.LastNotEnoughCultistsPopup) > TimeSpan.FromSeconds(1);

			if (allowPopup)
			{
				component.LastNotEnoughCultistsPopup = _timing.CurTime;
				_popupSystem.PopupEntity(
					Loc.GetString("cult-final-ritual-not-enough-cultists",
						("current", cultistsOnRunes.Count),
						("required", component.RequiredCultistsForChant)),
					user, user, PopupType.LargeCaution
				);
			}
			args.Handled = true;
			return;
		}

		// Ritual can begin
		component.RitualInProgress = true;
		component.LastNotEnoughCultistsPopup = _timing.CurTime;
		component.CurrentChantStep = 0;
		// Don't reset SacrificesCompleted - preserve it across ritual attempts for music offset
		component.RequiredSacrifices = 3;
		component.FinalSacrificeDone = false;
		component.TotalChantSteps = (SacrificeChantDelays.Length + 1) * component.RequiredSacrifices;
		component.ChantsCompletedInCycle = 0;
		component.PendingSacrifice = null;
		component.TimeUntilNextChant = 0f;
		component.FinalSacrificePending = false;
		component.TimeUntilNextShake = 0f;
		component.NextShakeIndex = 0;

		StartRitualMusic(riftUid, component);

		// Announce to all cultists
		AnnounceRitualStart();

		args.Handled = true;
	}

	/// <summary>
	/// When a cultist triggers a final summoning rune, check if enough cultists are on all 3 runes.
	/// </summary>
	private void OnTriggerRitual(EntityUid uid, FinalSummoningRuneComponent finalRune, TriggerEvent args)
	{
		// Only cultists can trigger the ritual
		if (args.User == null || !TryComp<BloodCultistComponent>(args.User, out var cultist))
		{
			args.Handled = true;
			return;
		}

		var user = args.User.Value;

		// Get the rift component
		if (finalRune.RiftUid == null || !TryComp<BloodCultRiftComponent>(finalRune.RiftUid, out var component))
		{
			args.Handled = true;
			return;
		}

		var riftUid = finalRune.RiftUid.Value;

		// If ritual is in progress but paused (music not playing), allow resuming
		if (component.RitualInProgress && component.RitualMusicPlaying)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-already-in-progress"),
				user, user, PopupType.MediumCaution
			);
			args.Handled = true;
			return;
		}

		// If ritual is paused (in progress but music stopped), resume it
		if (component.RitualInProgress && !component.RitualMusicPlaying)
		{
			// Check if enough cultists are present to resume
			var cultistsOnRunesForResume = GetCultistsOnSummoningRunes(component);
			if (cultistsOnRunesForResume.Count < component.RequiredCultistsForChant)
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-final-ritual-not-enough-cultists",
						("current", cultistsOnRunesForResume.Count),
						("required", component.RequiredCultistsForChant)),
					user, user, PopupType.LargeCaution
				);
				args.Handled = true;
				return;
			}

			// Resume the ritual - music will be restarted in Update loop at correct offset
			args.Handled = true;
			return;
		}

		// Ensure the cult has weakened the veil (blood collected plus ritual)
		if (!_bloodCultRule.TryGetActiveRule(out var ruleComp) || !ruleComp.VeilWeakened || ruleComp.BloodCollected < ruleComp.BloodRequiredForVeil)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-too-early",
					("collected", Math.Round(ruleComp.BloodCollected, 1)),
					("required", Math.Round(ruleComp.BloodRequiredForVeil, 1))),
				user, user, PopupType.LargeCaution
			);
			args.Handled = true;
			return;
		}

		component.RequiredCultistsForChant = 3;

		// Check if enough runes have cultists on them
		var cultistsOnRunes = GetCultistsOnSummoningRunes(component);
		if (cultistsOnRunes.Count < component.RequiredCultistsForChant)
		{
			var allowPopup = component.LastNotEnoughCultistsPopup == TimeSpan.Zero ||
				(_timing.CurTime - component.LastNotEnoughCultistsPopup) > TimeSpan.FromSeconds(1);

			if (allowPopup)
			{
				component.LastNotEnoughCultistsPopup = _timing.CurTime;
				_popupSystem.PopupEntity(
					Loc.GetString("cult-final-ritual-not-enough-cultists",
						("current", cultistsOnRunes.Count),
						("required", component.RequiredCultistsForChant)),
					user, user, PopupType.LargeCaution
				);
			}
			args.Handled = true;
			return;
		}

		// Ritual can begin!
		component.RitualInProgress = true;
		component.LastNotEnoughCultistsPopup = _timing.CurTime;
		component.CurrentChantStep = 0;
		// Don't reset SacrificesCompleted - preserve it across ritual attempts for music offset
		component.RequiredSacrifices = 3;
		component.FinalSacrificeDone = false;
		component.TotalChantSteps = (SacrificeChantDelays.Length + 1) * component.RequiredSacrifices;
		component.ChantsCompletedInCycle = 0;
		component.PendingSacrifice = null;
		component.TimeUntilNextChant = 0f;
		component.FinalSacrificePending = false;
		component.TimeUntilNextShake = 0f;
		component.NextShakeIndex = 0;

		StartRitualMusic(riftUid, component);

		// Announce to all cultists
		AnnounceRitualStart();

		args.Handled = true;
	}

	/// <summary>
	/// Processes a single chant step in the final ritual.
	/// </summary>
	private void ProcessChantStep(EntityUid runeUid, BloodCultRiftComponent component, TransformComponent xform)
	{
		// Count cultists on runes
		var cultistsOnRunes = GetCultistsOnSummoningRunes(component);
		var cultistCount = cultistsOnRunes.Count;

		if (cultistCount < component.RequiredCultistsForChant)
		{
			// Not enough cultists, ritual fails
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-not-enough-at-end",
					("current", cultistCount),
					("required", component.RequiredCultistsForChant)),
				runeUid, PopupType.LargeCaution
			);

			AnnounceRitualFailure();

			if (component.RitualMusicPlaying)
			{
				_sound.StopStationEventMusic(runeUid, StationEventMusicType.BloodCult);
				component.RitualMusicPlaying = false;
			}

			component.RitualInProgress = false;
			component.CurrentChantStep = 0;
			component.TimeUntilNextChant = 0f;
			component.ShakeDelays.Clear();
			component.NextShakeIndex = 0;
			component.TimeUntilNextShake = 0f;
			component.FinalSacrificePending = false;
			component.FinalSacrificeDone = false;
			// Don't reset SacrificesCompleted - preserve it across ritual attempts for music offset
			component.PendingSacrifice = null;
			component.ChantsCompletedInCycle = 0;
			// Don't reset the required cultists for chant. If people are dead let them keep the counter going.
			//component.RequiredCultistsForChant = 3;
			return;
		}

		EnsurePendingSacrifice(component, cultistsOnRunes);

		var chant = _bloodCultRule.GenerateChant(wordCount: 4); // Longer chants for final ritual

		bool hasPendingSacrifice = component.PendingSacrifice != null;
		var pendingUid = component.PendingSacrifice;
		bool shouldPromoteAllToLeaders = !hasPendingSacrifice && cultistCount == 1;

		foreach (var cultist in cultistsOnRunes)
		{
			if (!Exists(cultist))
				continue;

			var text = (component.PendingSacrifice == cultist || shouldPromoteAllToLeaders) ? chant : "Nar'Sie!";
			_bloodCultRule.Speak(cultist, text, forceLoud: true);
		}

		DoShake(runeUid, xform, FinalRitualShakeIntensity);

		// Increment chant step
		component.CurrentChantStep++;
		component.ChantsCompletedInCycle++;

		if (component.ChantsCompletedInCycle > SacrificeChantDelays.Length)
		{
			TryPerformFinalSacrifice(runeUid, component, xform);

			// Ritual may have ended inside TryPerformFinalSacrifice
			if (!component.RitualInProgress)
				return;
		}
		else
		{
			component.TimeUntilNextChant = SacrificeChantDelays[component.ChantsCompletedInCycle - 1];
		}

		// Check if ritual is complete
		if (component.CurrentChantStep >= component.TotalChantSteps)
		{
			if (component.SacrificesCompleted >= component.RequiredSacrifices)
			{
				// SUCCESS! Summon Nar'Sie
				SummonNarsie(runeUid, xform);
				AnnounceRitualSuccess();
				// Stop the music, and make sure there's no shake or chanting ongoing.
				// The shake and chanting should stop on their own when Nar'Sie eats them, but just incase.
				if (component.RitualMusicPlaying)
				{
					_sound.StopStationEventMusic(runeUid, StationEventMusicType.BloodCult);
					component.RitualMusicPlaying = false;
				}

				component.RitualInProgress = false;
				component.CurrentChantStep = 0;
				component.TimeUntilNextChant = 0f;
				component.ShakeDelays.Clear();
				component.NextShakeIndex = 0;
				component.TimeUntilNextShake = 0f;
				component.FinalSacrificePending = false;
				component.FinalSacrificeDone = false;
				component.SacrificesCompleted = 0; // Reset on successful completion
				component.PendingSacrifice = null;
				component.ChantsCompletedInCycle = 0;
			}
			else
			{
				component.CurrentChantStep = component.TotalChantSteps;
				component.TimeUntilNextChant = 1f;
			}
		}
	}

	/// <summary>
	/// Summons Nar'Sie at the rift location.
	/// </summary>
	private void SummonNarsie(EntityUid riftUid, TransformComponent xform)
	{
		var coordinates = xform.Coordinates;

		// Spawn Nar'Sie spawn animation
		var narsieSpawn = Spawn("MobNarsieSpawn", coordinates);

		// Mark all cultists as having summoned Nar'Sie
		// This probably is part of the reason the endgame counter doesn't work for how many cultists there are.
		// todo: fix the endgame counter
		var cultistQuery = EntityQueryEnumerator<BloodCultistComponent>();
		while (cultistQuery.MoveNext(out var cultistUid, out var cultist))
		{
			cultist.NarsieSummoned = coordinates;
		}

		// Announce to station
		_bloodCultRule.AnnounceNarsieSummon();
	}

	/// <summary>
	/// Gets a list of all live cultists currently standing on the summoning runes.
	/// This excludes soulstones, juggernauts, shades, ghosts, dead, and critical entities.
	/// Only living blood cultists with minds count towards the ritual requirement.
	/// </summary>
	private List<EntityUid> GetCultistsOnSummoningRunes(BloodCultRiftComponent riftComp)
	{
		var cultists = new HashSet<EntityUid>();

		foreach (var runeUid in riftComp.SummoningRunes)
		{
			if (!Exists(runeUid) || !TryComp(runeUid, out TransformComponent? runeXform))
				continue;

			// Look for cultists near this rune
			// The FinalRiftRune is 3x3 tiles, so we need a range that covers from center to corner
			// 3 tiles = 3 meters, so diagonal distance from center to corner is ~2.12 meters
			// Using 2.0f to ensure we cover the entire 3x3 area with some margin
			var nearbyEntities = _lookup.GetEntitiesInRange(runeXform.Coordinates, 2.0f);
			foreach (var entity in nearbyEntities)
			{
				// IsValidSummoningParticipant filters out:
				// - Non-cultists
				// - Soulstones (SoulStoneComponent)
				// - Juggernauts (JuggernautComponent)
				// - Shades (ShadeComponent)
				// - Dead or critical entities
				// - Entities without minds
				if (!IsValidSummoningParticipant(entity))
					continue;

				cultists.Add(entity);
			}
		}

		// Additional safety check: remove any ghosts that might have slipped through
		var result = cultists.ToList();
		result.RemoveAll(uid => HasComp<GhostComponent>(uid));
		return result;
	}


	private void EnsurePendingSacrifice(BloodCultRiftComponent component, List<EntityUid> cultists)
	{
		// Check if current pending sacrifice is still valid
		if (component.PendingSacrifice is { } pending)
		{
			// If pending sacrifice is still in the list and is still a valid participant, keep it
			if (cultists.Contains(pending) && IsValidSummoningParticipant(pending))
				return;
			
			// Otherwise, clear it (entity may have become invalid - e.g., soulstone, juggernaut, shade, or died)
			component.PendingSacrifice = null;
		}

		if (cultists.Count == 0)
		{
			component.PendingSacrifice = null;
			return;
		}

		component.PendingSacrifice = _random.Pick(cultists);
	}

	private void StartRitualMusic(EntityUid riftUid, BloodCultRiftComponent component)
	{
		if (component.RitualMusicPlaying)
			return;

		var resolved = _audio.ResolveSound(component.RitualMusic);
		if (ResolvedSoundSpecifier.IsNullOrEmpty(resolved))
			return;

		// Get audio length and store it for music-synced sacrifices
		var audioLength = _audio.GetAudioLength(resolved);
		component.RitualMusicDuration = (float)audioLength.TotalSeconds;
		component.RitualStartTime = _timing.CurTime;
		
		// Initialize sacrifice cooldown to allow first sacrifice immediately
		// This will be updated after each sacrifice to enforce 5-second minimum
		component.TimeSinceLastSacrifice = TimeSpan.Zero;

		// Calculate playback offset based on sacrifice progress
		float offset = 0f;
		if (component.RequiredSacrifices > 0 && component.SacrificesCompleted > 0)
		{
			var progress = (float)component.SacrificesCompleted / component.RequiredSacrifices;
			offset = component.RitualMusicDuration * progress;
			// Clamp offset to valid range
			offset = Math.Clamp(offset, 0f, component.RitualMusicDuration);
		}

		// Note: DispatchStationEventMusic doesn't accept AudioParams directly
		// The offset is handled by the music track itself
		_sound.DispatchStationEventMusic(riftUid, resolved, StationEventMusicType.BloodCult);
		component.RitualMusicPlaying = true;

		// Initialize timing for "Nar'Sie" chants and shake-synced chants
		component.TimeUntilNextNarsieChant = 3f;
		component.TimeUntilNextShakeForChant = 5f;
	}

	/// <summary>
	/// Makes non-sacrifice cultists chant "Nar'Sie!" every 3 seconds.
	/// </summary>
	private void DoNarsieChant(EntityUid riftUid, BloodCultRiftComponent component, List<EntityUid> cultistsOnRunes)
	{
		foreach (var cultist in cultistsOnRunes)
		{
			if (!Exists(cultist))
				continue;

			// Only non-sacrifice cultists chant "Nar'Sie"
			if (component.PendingSacrifice != cultist)
			{
				_bloodCultRule.Speak(cultist, "Nar'Sie!", forceLoud: true);
			}
		}
	}

	/// <summary>
	/// Performs a shake effect and makes the sacrifice victim chant a longer phrase, synced with the shake.
	/// </summary>
	private void DoShakeWithLongChant(EntityUid riftUid, BloodCultRiftComponent component, TransformComponent xform, List<EntityUid> cultistsOnRunes)
	{
		// Perform the shake effect
		DoShake(riftUid, xform, FinalRitualShakeIntensity);

		// Only the sacrifice victim chants the longer phrase
		if (component.PendingSacrifice is { } pending && cultistsOnRunes.Contains(pending))
		{
			var chant = _bloodCultRule.GenerateChant(wordCount: 4); // Longer chants for final ritual
			_bloodCultRule.Speak(pending, chant, forceLoud: true);
		}
		else if (cultistsOnRunes.Count == 1)
		{
			// If only one cultist, they get the longer chant
			var chant = _bloodCultRule.GenerateChant(wordCount: 4);
			_bloodCultRule.Speak(cultistsOnRunes[0], chant, forceLoud: true);
		}
	}

	private void AnnounceSacrificeProgress(int completed)
	{
		string? message = completed switch
		{
			1 => "The first sacrifice is complete. Nar'Sie begins to enter our reality.",
			2 => "The second sacrifice is complete. The Geometer of Blood pries open the veil.",
			3 => "The final sacrifice is complete. She. Is. Here.",
			_ => null
		};

		if (message == null)
			return;

		_chatSystem.DispatchGlobalAnnouncement(message, "Unknown", playSound: true, colorOverride: Color.DarkRed);
	}

	/// <summary>
	/// Determines if an entity is a valid participant for the final summoning ritual.
	/// Only living blood cultists with minds are valid. Soulstones, juggernauts, shades,
	/// ghosts, dead entities, and critical entities are excluded.
	/// </summary>
	private bool IsValidSummoningParticipant(EntityUid entity)
	{
		// Must be a blood cultist
		if (!HasComp<BloodCultistComponent>(entity))
			return false;

		// Cannot be a soulstone, juggernaut, or shade - only living cultists count
		// These constructs are created from sacrificed cultists and should not count
		if (HasComp<SoulStoneComponent>(entity) || HasComp<JuggernautComponent>(entity) || HasComp<ShadeComponent>(entity))
			return false;

		// Must be alive and not critical
		if (_mobState.IsDead(entity) || _mobState.IsCritical(entity))
			return false;

		// Must have a mind (ensures it's a player-controlled entity, not an NPC or construct)
		if (!TryComp<MindContainerComponent>(entity, out var mind) || mind.Mind == null)
			return false;

		return true;
	}

	private void SpillOverflow(EntityUid riftUid, FixedPoint2 overflow)
	{
		if (overflow <= FixedPoint2.Zero)
			return;

		var solution = new Solution("UnholyBlood", overflow);
		_puddleSystem.TrySpillAt(Transform(riftUid).Coordinates, solution, out _);
	}

	private void AnnounceRitualStart()
	{
		var cultistQuery = EntityQueryEnumerator<BloodCultistComponent>();
		while (cultistQuery.MoveNext(out var cultistUid, out var _))
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-started"),
				cultistUid, cultistUid, PopupType.LargeCaution
			);
		}
	}

	private void AnnounceRitualFailure()
	{
		var cultistQuery = EntityQueryEnumerator<BloodCultistComponent>();
		while (cultistQuery.MoveNext(out var cultistUid, out var _))
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-failed"),
				cultistUid, cultistUid, PopupType.LargeCaution
			);
		}
	}

	private void AnnounceRitualSuccess()
	{
		var cultistQuery = EntityQueryEnumerator<BloodCultistComponent>();
		while (cultistQuery.MoveNext(out var cultistUid, out var _))
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-final-ritual-success"),
				cultistUid, cultistUid, PopupType.LargeCaution
			);
		}
	}
}