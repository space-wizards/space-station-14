// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Numerics;
using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Trigger;
using Content.Shared.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.BloodCult;
using Content.Server.BloodCult.Components;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mindshield.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Organ;
using Content.Shared.Roles;
using Content.Server.Roles;
using Content.Server.Mind;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Server.GameTicking;
using Content.Shared.Stunnable;
using Content.Shared.StatusEffect;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Tag;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Shared.GameTicking.Components;
using Content.Shared.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Emoting;
using Content.Shared.NPC.Systems;

namespace Content.Server.BloodCult.EntitySystems
{
	public sealed partial class OfferOnTriggerSystem : EntitySystem
	{
		private const string MindShieldTag = "MindShield";

		[Dependency] private readonly PopupSystem _popupSystem = default!;
		[Dependency] private readonly EntityLookupSystem _lookup = default!;
		[Dependency] private readonly MobStateSystem _mobState = default!;
		[Dependency] private readonly SharedRoleSystem _role = default!;
		[Dependency] private readonly BloodCultistSystem _bloodCultist = default!;
		[Dependency] private readonly MindSystem _mind = default!;
		[Dependency] private readonly SharedAudioSystem _audio = default!;
		[Dependency] private readonly GameTicker _gameTicker = default!;
		[Dependency] private readonly SharedContainerSystem _container = default!;
		[Dependency] private readonly BloodstreamSystem _bloodstream = default!;
		[Dependency] private readonly SharedStunSystem _stun = default!;
		[Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;
		[Dependency] private readonly BodySystem _bodySystem = default!;
		[Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
		[Dependency] private readonly IGameTiming _gameTiming = default!;
		[Dependency] private readonly ChatSystem _chat = default!;
		[Dependency] private readonly SharedTransformSystem _transform = default!;
		[Dependency] private readonly DamageableSystem _damageable = default!;
		[Dependency] private readonly IPrototypeManager _prototypeManager = default!;
		[Dependency] private readonly TagSystem _tag = default!;
		[Dependency] private readonly SharedSubdermalImplantSystem _implantSystem = default!;
		[Dependency] private readonly SharedPhysicsSystem _physics = default!;
		[Dependency] private readonly IRobustRandom _random = default!;
		[Dependency] private readonly NpcFactionSystem _npcFaction = default!;

		private static readonly ProtoId<DamageTypePrototype> SlashDamageType = "Slash";

		public override void Initialize()
		{
		base.Initialize();
		SubscribeLocalEvent<OfferOnTriggerComponent, TriggerEvent>(HandleOfferTrigger);
		SubscribeLocalEvent<BloodCultistComponent, MindshieldBreakDoAfterEvent>(OnMindshieldBreakComplete);
		}

	public override void Update(float frameTime)
	{
		base.Update(frameTime);

		var curTime = _gameTiming.CurTime;
		var query = EntityQueryEnumerator<MindshieldBreakRitualComponent>();
		while (query.MoveNext(out var uid, out var ritual))
		{
			// Validate ritual conditions BEFORE chanting
			// This ensures multiplayer robustness
			
			// Check if victim still exists
			if (!Exists(ritual.Victim))
			{
				_FailRitual(uid, "cult-invocation-interrupted");
				continue;
			}
			
			// Validate all participants are still alive, in range, and not deleted
			var runePos = _transform.ToMapCoordinates(ritual.RuneLocation).Position;
			var validParticipants = 0;
			
			foreach (var participant in ritual.Participants)
			{
				// Check if participant exists
				if (!Exists(participant))
					continue;
				
				// Check if participant is dead
				if (_mobState.IsDead(participant))
					continue;
				
				// Check if participant is in range of the rune (2.5m)
				var participantPos = _transform.GetWorldPosition(participant);
				if ((participantPos - runePos).Length() > 2.5f)
					continue;
				
				validParticipants++;
			}
			
			// Need at least 3 valid participants to continue the ritual
			if (validParticipants < 3)
			{
				_FailRitual(uid, "cult-invocation-fail");
				continue;
			}
			
			// Check if it's time to chant
			if (curTime >= ritual.NextChantTime && ritual.ChantCount < 3)
			{
				ritual.ChantCount++;
				ritual.NextChantTime = curTime + TimeSpan.FromSeconds(2);

				// Make only the minimum required participants chant (first 3 valid cultists)
				int chantersCount = 0;
				const int requiredChanters = 3;
				
				foreach (var participant in ritual.Participants)
				{
					if (chantersCount >= requiredChanters)
						break;
					
					if (!Exists(participant) || _mobState.IsDead(participant))
						continue;
					
					var participantPos = _transform.GetWorldPosition(participant);
					if ((participantPos - runePos).Length() > 2.5f)
						continue;

					// Generate random cult chant (2 words)
					var chant = _bloodCultRule.GenerateChant(wordCount: 2);
					var chatType = _gameTicker.IsGameRuleActive<BloodCultRuleComponent>() ? InGameICChatType.Speak : InGameICChatType.Whisper;
					_chat.TrySendInGameICMessage(participant, chant, chatType, false);
					chantersCount++;
				}
			}
		}
	}
	
	/// <summary>
	/// Cancels a mindshield breaking ritual and shows failure message to all participants
	/// </summary>
	private void _FailRitual(EntityUid ritualist, string localizationKey)
	{
		// Get the ritual component to show message to all participants
		EntityUid[] participants = Array.Empty<EntityUid>();
		if (TryComp<MindshieldBreakRitualComponent>(ritualist, out var ritual))
		{
			participants = ritual.Participants;
		}
		
		// Show failure message to all participants
		foreach (var participant in participants)
		{
			if (!Exists(participant))
				continue;
			
			_popupSystem.PopupEntity(
				Loc.GetString(localizationKey),
				participant, participant, PopupType.MediumCaution
			);
		}
		
		// If no participants (component already removed), at least show to ritualist
		if (participants.Length == 0)
		{
			_popupSystem.PopupEntity(
				Loc.GetString(localizationKey),
				ritualist, ritualist, PopupType.MediumCaution
			);
		}
		
		// Cancel any MindshieldBreakDoAfterEvent DoAfters on the ritualist
		if (TryComp<DoAfterComponent>(ritualist, out var doAfterComp))
		{
			// Find and cancel the mindshield breaking DoAfter
			foreach (var (id, doAfter) in doAfterComp.DoAfters)
			{
				if (doAfter.Args.Event is MindshieldBreakDoAfterEvent)
				{
					_doAfter.Cancel(ritualist, id, doAfterComp);
					break;
				}
			}
		}
		
		// Remove the ritual component
		// This ensures the completion handler will return early if it somehow still fires
		RemCompDeferred<MindshieldBreakRitualComponent>(ritualist);
	}

		private void HandleOfferTrigger(EntityUid uid, OfferOnTriggerComponent component, TriggerEvent args)
		{
			if (args.Handled || args.User == null)
				return;
			EntityUid user = (EntityUid)args.User;

			if (!TryComp(user, out BloodCultistComponent? bloodCultist))
				return;

		var offerLookup = _lookup.GetEntitiesInRange(uid, component.OfferRange);
		var invokeLookup = _lookup.GetEntitiesInRange(uid, component.InvokeRange);
		EntityUid[] cultistsInRange = Array.FindAll(invokeLookup.ToArray(), item => 
		{
			// Must be a cultist or construct
			if (!HasComp<BloodCultistComponent>(item) && !HasComp<BloodCultConstructComponent>(item))
				return false;
			
			// Must not be dead
			if (_mobState.IsDead(item))
				return false;
			
			// Regular cultists must have a BodyComponent (excludes detached heads/brain items)
			// Constructs are allowed without BodyComponent check
			if (HasComp<BloodCultistComponent>(item) && !HasComp<BloodCultConstructComponent>(item))
			{
				if (!HasComp<BodyComponent>(item))
					return false;
			}
			
			return true;
		});

		// Find any entity with a soul (simplified candidate selection)
		EntityUid? candidate = null;
		foreach (var look in offerLookup)
		{
			// Skip if already a cultist
			if (HasComp<BloodCultistComponent>(look))
				continue;
			
			// Check if it has a soul
			if (_IsValidTarget(look, out var _))
			{
				candidate = look;
				break; // Take the first valid target found
			}
		}

		if (candidate != null)
		{
			EntityUid offerable = (EntityUid) candidate;

			// Validate target has a soul (should already be checked, but double-check for safety)
			if (!_IsValidTarget(offerable, out var mind))
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-invocation-fail-nosoul"),
					user, user, PopupType.MediumCaution
				);
				args.Handled = true;
				return;
			}
			// Check if already a cultist
			if (HasComp<BloodCultistComponent>(offerable) || (mind != null && _role.MindHasRole<BloodCultRoleComponent>((EntityUid)mind)))
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-invocation-fail-teamkill"),
					user, user, PopupType.MediumCaution
				);
				args.Handled = true;
				return;
			}
			// If mindshielded, break the mindshield first (conversion will happen after breaking)
			if (HasComp<MindShieldComponent>(offerable))
			{
				_BreakMindshield(offerable, user, cultistsInRange, Transform(uid).Coordinates, uid);
				args.Handled = true;
				return;
			}
			// If humanoid and can bleed, convert
			if (HasComp<HumanoidAppearanceComponent>(offerable) && TryComp<BloodstreamComponent>(offerable, out var offerableBloodstream) && _CanBeConverted(offerable))
			{
				_bloodCultist.UseConvertRune(offerable, user, uid, cultistsInRange);
				
				// Add blood to the ritual pool based on the victim's current blood level
				// If they're at 50% blood, only add 50u instead of 100u
				// Also account for blood already spilled from EdgeEssentia wounds
				var bloodPercentage = _bloodstream.GetBloodLevel((offerable, offerableBloodstream));
				var bloodFromConversion = 100.0 * bloodPercentage;
				
				// Check if this entity has already contributed blood via EdgeEssentia bleeding
				var alreadyContributed = 0.0;
				if (TryComp<BloodCollectionTrackerComponent>(offerable, out var tracker))
				{
					alreadyContributed = tracker.TotalBloodCollected;
				}
				
				// Ensure total contribution from this entity never exceeds 100 units
				var remainingAllowance = Math.Max(0, 100.0 - alreadyContributed);
				var bloodToAdd = Math.Min(bloodFromConversion, remainingAllowance);
				
				if (bloodToAdd > 0)
				{
					_bloodCultRule.AddBloodForConversion(bloodToAdd);
					
					// Update the tracker
					var conversionTracker = EnsureComp<BloodCollectionTrackerComponent>(offerable);
					conversionTracker.TotalBloodCollected = Math.Min(conversionTracker.TotalBloodCollected + (float)bloodToAdd, conversionTracker.MaxBloodPerEntity);
				}
				
				args.Handled = true;
				return;
			}
			// If hamlet, make a hamlet soulstone (requires 2 cultists total: user + 1 other)
			// Note: cultistsInRange includes all cultists in range, including the user
			if (_IsHamlet(offerable))
			{
				if (cultistsInRange.Length < 2)
				{
					_popupSystem.PopupEntity(
						Loc.GetString("cult-invocation-fail"),
						user, user, PopupType.MediumCaution
					);
				}
				else
				{
					_CreateSoulstoneFromEntity(offerable, user, uid, cultistsInRange);
				}
				
				args.Handled = true;
				return;
			}
			// If has brain prototype (borg, animal with brain, etc.), soulstone it
			else if (_HasBrainPrototype(offerable))
			{
				_CreateSoulstoneFromEntity(offerable, user, uid, cultistsInRange);
			}
			else
			{
				// Entity cannot be converted, soulstoned, or sacrificed
				_popupSystem.PopupEntity(
					Loc.GetString("cult-invocation-fail"),
					user, user, PopupType.MediumCaution
				);
			}
		}
		else
		{
			// No valid target found
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail"),
				user, user, PopupType.MediumCaution
			);
		}
		args.Handled = true;
	}


		private bool _IsSacrificeTarget(EntityUid target, BloodCultistComponent comp)
		{
			return comp.Targets.Contains(target);
		}

		private bool _IsValidTarget(EntityUid uid, out Entity<MindComponent>? mind)
		{
			mind = null;
			
			// Soulstones cannot be sacrificed or converted
			if (HasComp<SoulStoneComponent>(uid))
				return false;
			
			if (TryComp(uid, out MindContainerComponent? mindContainer) &&
				mindContainer.Mind != null &&
				TryComp((EntityUid)mindContainer.Mind, out MindComponent? mindComponent))
				mind = ((EntityUid)mindContainer.Mind, (MindComponent) mindComponent);
			return mind != null;  // must have a soul
		}

	private bool _CanBeConverted(EntityUid uid)
	{
		// Regular entities must be alive and not mindshielded
		return !_mobState.IsDead(uid) &&  // must not be dead
			!HasComp<MindShieldComponent>(uid);  // must not be mindshielded
	}

	/// <summary>
	/// Checks if an entity is Hamlet (the special hamster)
	/// </summary>
	private bool _IsHamlet(EntityUid uid)
	{
		var meta = MetaData(uid);
		return meta.EntityPrototype?.ID == "MobHamsterHamlet";
	}

	/// <summary>
	/// Checks if an entity has a brain prototype that can be extracted (borgs, animals with brains, etc.)
	/// </summary>
	private bool _HasBrainPrototype(EntityUid uid)
	{
		// Borgs have brains in their brain container
		if (HasComp<BorgChassisComponent>(uid))
			return true;
		
		// Standalone brains (not in containers)
		if (HasComp<BrainComponent>(uid) && !_container.IsEntityInContainer(uid))
			return true;
		
		// Detached heads with brains
		if (TryComp<BodyPartComponent>(uid, out var bodyPart) && bodyPart.PartType == BodyPartType.Head)
		{
			if (!_container.IsEntityInContainer(uid) && TryComp<BodyComponent>(uid, out var headBody))
			{
				var headBrains = _bodySystem.GetBodyOrganEntityComps<BrainComponent>((uid, headBody));
				if (headBrains.Count > 0)
					return true;
			}
		}
		
		// Entities with BodyComponent containing brain organs (e.g., animals, Diona Brain Nymphs)
		// Exclude humanoids - they should be converted instead
		if (!_container.IsEntityInContainer(uid) && !HasComp<HumanoidAppearanceComponent>(uid) && TryComp<BodyComponent>(uid, out var body))
		{
			var bodyBrains = _bodySystem.GetBodyOrganEntityComps<BrainComponent>((uid, body));
			if (bodyBrains.Count > 0)
				return true;
		}
		
		return false;
	}

	private bool _IsSoulstoneEligible(EntityUid uid)
	{
		// Entities that cannot bleed (no bloodstream) should be captured in soulstones
		// This includes borgs, slimes, and other non-organic entities
		
		// disabled:Cyborgs can only be soulstoned when in critical state
		// Decided it would be easier to just allow cyborgs to be soulstoned even if they're not crit.
		// Couldn't figure out a lore reason that Nar'Sie would be stopped by their battery being powered.
		if (HasComp<BorgChassisComponent>(uid))
			return true;
		
		// Standalone brains (not in containers) should always be soulstone-eligible
		if (HasComp<BrainComponent>(uid) && !_container.IsEntityInContainer(uid))
			return true;
		
		// Detached heads with brains should be soulstone-eligible
		if (TryComp<BodyPartComponent>(uid, out var bodyPart) && bodyPart.PartType == BodyPartType.Head)
		{
			if (!_container.IsEntityInContainer(uid) && TryComp<BodyComponent>(uid, out var headBody))
			{
				var headBrains = _bodySystem.GetBodyOrganEntityComps<BrainComponent>((uid, headBody));
				if (headBrains.Count > 0)
					return true;
			}
		}
		
		// Entities with BodyComponent containing brain organs (e.g., Diona Brain Nymphs) should be soulstone-eligible
		// BUT exclude humanoids - they should be converted instead
		if (!_container.IsEntityInContainer(uid) && !HasComp<HumanoidAppearanceComponent>(uid) && TryComp<BodyComponent>(uid, out var body))
		{
			var bodyBrains = _bodySystem.GetBodyOrganEntityComps<BrainComponent>((uid, body));
			if (bodyBrains.Count > 0)
				return true;
		}
		
		return !HasComp<BloodstreamComponent>(uid);
	}

	private void _CreateSoulstoneFromEntity(EntityUid victim, EntityUid user, EntityUid rune, EntityUid[] cultistsInRange)
	{
		// Soulstone creation only requires the user (who is already validated as a cultist)
		// The user is always present since they're the one triggering the rune
		// No need to check cultistsInRange - the user alone is sufficient
		
		var coordinates = Transform(victim).Coordinates;
		CreateSoulstoneInternal(victim, coordinates, user, true);
	}

	public bool TryForceSoulstoneCreation(EntityUid victim, EntityCoordinates coordinates)
	{
		return CreateSoulstoneInternal(victim, coordinates, null, false, true);
	}

	private bool CreateSoulstoneInternal(EntityUid victim, EntityCoordinates coordinates, EntityUid? user, bool showPopup, bool autoActivateShade = false)
	{
		EntityUid? mindId = CompOrNull<MindContainerComponent>(victim)?.Mind;
		MindComponent? mindComp = mindId != null ? CompOrNull<MindComponent>(mindId) : null;

		if (mindId == null || mindComp == null)
		{
			if (showPopup && user != null)
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-invocation-fail-nosoul"),
					user.Value, user.Value, PopupType.MediumCaution
				);
			}
			return false;
		}

		EntProtoId? originalEntityPrototype = null;

		var brainRemoved = false;

		// Check if the victim is Hamlet - if so, use the victim's prototype instead of the brain's
		var victimMeta = MetaData(victim);
		var isHamlet = victimMeta.EntityPrototype?.ID == "MobHamsterHamlet";
		// Capture speech components from Hamlet before victim might be deleted
		SpeechComponent? victimSpeech = null;
		ReplacementAccentComponent? victimAccent = null;
		if (isHamlet)
		{
			TryComp<SpeechComponent>(victim, out victimSpeech);
			TryComp<ReplacementAccentComponent>(victim, out victimAccent);
		}

		if (HasComp<BorgChassisComponent>(victim))
		{
			if (_container.TryGetContainer(victim, "borg_brain", out var brainContainer))
			{
				foreach (var contained in brainContainer.ContainedEntities)
				{
					var brainUid = contained;
					var brainMeta = MetaData(brainUid);
					if (brainMeta.EntityPrototype != null && !isHamlet)
						originalEntityPrototype = brainMeta.EntityPrototype.ID;

					_container.Remove(brainUid, brainContainer);
					QueueDel(brainUid);
					brainRemoved = true;
					break;
				}
			}
		}

		if (!brainRemoved && TryComp<BodyComponent>(victim, out var body))
		{
			var brains = _bodySystem.GetBodyOrganEntityComps<BrainComponent>((victim, body));
			foreach (var (brainUid, brainComp, organComp) in brains)
			{
				var brainMeta = MetaData(brainUid);
				if (brainMeta.EntityPrototype != null && !isHamlet)
					originalEntityPrototype = brainMeta.EntityPrototype.ID;

				_bodySystem.RemoveOrgan(brainUid, organComp);
				QueueDel(brainUid);
				brainRemoved = true;
				break;
			}

			// Delete the victim after extracting the brain
			if (brainRemoved)
			{
				QueueDel(victim);
			}
			// If no brain was found, check if this is a skeleton (skeletons don't have brain organs, the head IS the mind container)
			else if (!brainRemoved)
			{
				// Check if victim has a head part (skeleton heads are the mind container, not brain organs)
				var headParts = _bodySystem.GetBodyChildrenOfType(victim, BodyPartType.Head, body);
				foreach (var (headUid, headPart) in headParts)
				{
					var headMeta = MetaData(headUid);
					if (headMeta.EntityPrototype != null && !isHamlet)
					{
						// Store the head part prototype (e.g., HeadSkeleton) for respawning
						originalEntityPrototype = headMeta.EntityPrototype.ID;
						break;
					}
				}

				QueueDel(victim);
			}
		}
		else if (!brainRemoved)
		{
			// Check if the victim is already a detached head (like HeadSkeleton)
			// Get the prototype (both branches do the same thing)
			if (victimMeta.EntityPrototype != null)
				originalEntityPrototype = victimMeta.EntityPrototype.ID;

			QueueDel(victim);
		}

		// If Hamlet, ensure we use the victim's prototype
		if (isHamlet && victimMeta.EntityPrototype != null)
			originalEntityPrototype = victimMeta.EntityPrototype.ID;

		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/disintegrate.ogg"), coordinates);

		var soulstonePrototype = isHamlet
			? "CultHamstone"
			: (autoActivateShade ? "CultSoulStoneShard" : "CultSoulStone");
		var soulstone = Spawn(soulstonePrototype, coordinates);
		_mind.TransferTo(mindId.Value, soulstone, mind: mindComp);

		// Preserve speech component and speech restrictions (ReplacementAccentComponent) from Hamlet if applicable
		if (isHamlet && victimSpeech != null)
		{
			CopyComp(victim, soulstone, victimSpeech);
			// Copy ReplacementAccentComponent if it exists (preserves speech restrictions like cognizine requirement)
			if (victimAccent != null)
			{
				CopyComp(victim, soulstone, victimAccent);
			}
		}
		else
		{
			EnsureComp<SpeechComponent>(soulstone);
		}
		EnsureComp<EmotingComponent>(soulstone);

		if (TryComp<SoulStoneComponent>(soulstone, out var soulstoneComp) && originalEntityPrototype != null)
		{
			soulstoneComp.OriginalEntityPrototype = originalEntityPrototype;
			Dirty(soulstone, soulstoneComp);
		}

		// This gives the soulstone a tiny nudge.
		// It makes it quite a bit more visually interestina and draws the eye to it rather than it being hidden under the body.
		if (TryComp<PhysicsComponent>(soulstone, out var physics))
		{
			_physics.SetAwake((soulstone, physics), true);
			var randomDirection = _random.NextVector2();
			var speed = _random.NextFloat(5f, 10f);
			var impulse = randomDirection * speed * physics.Mass;
			_physics.ApplyLinearImpulse(soulstone, impulse, body: physics);
		}

		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), coordinates);

		// Only used during the final ritual, so the soulstones made from dead cultists don't just spawn and sit there.
		if (autoActivateShade && TryComp<MindContainerComponent>(soulstone, out var soulstoneMind) && soulstoneMind.Mind != null)
		{
			if (TryComp<MindComponent>((EntityUid)soulstoneMind.Mind, out var mind))
			{
				var shade = Spawn("MobBloodCultShade", coordinates);
				_mind.TransferTo((EntityUid)soulstoneMind.Mind, shade, mind: mind);

				var shadeCultist = EnsureComp<BloodCultistComponent>(shade);

				if (_bloodCultRule.TryGetActiveRule(out var activeRule))
				{
					shadeCultist.ShowTearVeilRune = activeRule.HasRisen || activeRule.VeilWeakened;
					shadeCultist.LocationForSummon = activeRule.LocationForSummon;
					Dirty(shade, shadeCultist);
				}

				_npcFaction.AddFaction(shade, BloodCultRuleSystem.BloodCultistFactionId);

				if (TryComp<ShadeComponent>(shade, out var shadeComp))
					shadeComp.SourceSoulstone = soulstone;

				_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), coordinates);
			}
		}

		if (showPopup && user != null)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-soulstone-created"),
				user.Value, user.Value, PopupType.Large
			);
		}

		return true;
	}

	// Disabled - cultists should revive and convert instead
	// private bool _CanBeSacrificed(EntityUid uid, List<EntityUid> shells)
	// {
	// 	// Sacrifice requires a juggernaut shell to be present
	// 	return shells.Count > 0;
	// }

	private void _BreakMindshield(EntityUid victim, EntityUid user, EntityUid[] cultistsInRange, EntityCoordinates runeLocation, EntityUid runeEntity)
	{
		// Check if the cult has reached stage 2 (HasRisen) - required to break mindshields
		var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
		BloodCultRuleComponent? cultRule = null;
		while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
		{
			cultRule = ruleComp;
			break;
		}

		if (cultRule != null && !cultRule.HasRisen)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-mindshield-too-early"),
				user, user, PopupType.LargeCaution
			);
			return;
		}

		// Require 3 cultists total (user + 2 others)
		if (cultistsInRange.Length < 3)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail"),
				user, user, PopupType.MediumCaution
			);
			return;
		}

		// Validate victim is still valid and has a mindshield before starting the ritual
		// This ensures we're targeting the correct entity and prevents issues with multiple entities
		if (!Exists(victim))
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail"),
				user, user, PopupType.MediumCaution
			);
			return;
		}
		
		if (!TryComp<MindShieldComponent>(victim, out var _))
		{
			// Victim no longer has mindshield or was the wrong entity
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail"),
				user, user, PopupType.MediumCaution
			);
			return;
		}

		// Start the 6 second ritual with DoAfter
		// The victim stored in the ritual component will be the source of truth
		var doAfterEvent = new MindshieldBreakDoAfterEvent(victim, cultistsInRange, runeLocation);
		var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(6), doAfterEvent, user, target: victim)
		{
			BreakOnMove = true,
			BreakOnDamage = true,
			NeedHand = false,
			DistanceThreshold = 2.5f,
			AttemptFrequency = AttemptFrequency.EveryTick, // Check every tick to validate participants
			// Custom validation will check if other cultists are still in range
		};

		if (!_doAfter.TryStartDoAfter(doAfterArgs))
			return;

		// Create ritual tracking component on the user to handle periodic chanting
		// Store the victim here - this is the source of truth for which entity to target
		// This ensures we always target the original entity even if multiple entities are present
		var ritual = EnsureComp<MindshieldBreakRitualComponent>(user);
		ritual.Victim = victim; // Store the original victim - this will be used when the ritual completes
		ritual.Participants = cultistsInRange;
		ritual.RuneLocation = runeLocation;
		ritual.RuneEntity = runeEntity;
		ritual.StartTime = _gameTiming.CurTime;
		ritual.NextChantTime = _gameTiming.CurTime; // Chant immediately
		ritual.ChantCount = 0;

		// Show dramatic start message to all cultists
		foreach (EntityUid cultist in cultistsInRange)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-mindshield-break"),
				cultist, cultist, PopupType.LargeCaution
			);
		}
	}

	private void OnMindshieldBreakComplete(Entity<BloodCultistComponent> cultist, ref MindshieldBreakDoAfterEvent args)
	{
		if (args.Handled)
			return;

		var user = cultist.Owner;

		// Safety check: ensure user still exists
		if (!Exists(user))
			return;

		// Get data from the ritual tracking component - this is the source of truth for the victim
		if (!TryComp<MindshieldBreakRitualComponent>(user, out var ritual))
		{
			// Component was removed early, ritual failed
			return;
		}

		// Use the victim stored in the ritual component - this ensures we target the original entity
		// even if multiple entities are present or the DoAfter target changes
		var victim = ritual.Victim;
		var participants = ritual.Participants;
		var runeLocation = ritual.RuneLocation;
		var runeEntity = ritual.RuneEntity;

	// Remove the ritual tracking component
	RemCompDeferred<MindshieldBreakRitualComponent>(user);

	// If the ritual was interrupted (user moved, took damage, etc.), show failure message to all participants
	if (args.Cancelled)
	{
		foreach (var participant in participants)
		{
			if (!Exists(participant))
				continue;
			
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-interrupted"),
				participant, participant, PopupType.MediumCaution
			);
		}
		return;
	}

	// Safety check: ensure victim still exists (could be deleted, gibbed, etc. during DoAfter)
	// This validates the victim stored in the ritual component is still valid
	if (!Exists(victim))
	{
		foreach (var participant in participants)
		{
			if (!Exists(participant))
				continue;
			
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-interrupted"),
				participant, participant, PopupType.MediumCaution
			);
		}
		args.Handled = true;
		return;
	}
	
	// Additional validation: ensure the victim still has a MindShieldComponent
	// This prevents trying to remove mindshield from the wrong entity or an entity that lost its mindshield
	if (!TryComp<MindShieldComponent>(victim, out var _))
	{
		// Victim no longer has mindshield - might have been removed or wrong entity
		foreach (var participant in participants)
		{
			if (!Exists(participant))
				continue;
			
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-interrupted"),
				participant, participant, PopupType.MediumCaution
			);
		}
		args.Handled = true;
		return;
	}

	// Validate that all participants are still present and in range
	var validParticipants = new List<EntityUid>();
	foreach (var participant in participants)
	{
		if (!Exists(participant) || _mobState.IsDead(participant))
			continue;

		var participantPos = _transform.GetWorldPosition(participant);
		var runePos = _transform.ToMapCoordinates(runeLocation).Position;
		if ((participantPos - runePos).Length() > 2.5f)
			continue;

		validParticipants.Add(participant);
	}

	// Still need 3 cultists at the end
	if (validParticipants.Count < 3)
	{
		// Show failure message to all participants who are still around
		foreach (var participant in participants)
		{
			if (!Exists(participant))
				continue;
			
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-interrupted"),
				participant, participant, PopupType.MediumCaution
			);
		}
		return;
	}

		var coordinates = Transform(victim).Coordinates;
		
		// Play dramatic completion audio
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Ambience/Antag/creepyshriek.ogg"), coordinates);
		
	// Apply damage, bleeding, and stun to ONLY the participating cultists
	foreach (EntityUid participant in validParticipants)
	{
		// Apply slash damage (20 points) - the ritual tears at their flesh
		var damageSpec = new DamageSpecifier(_prototypeManager.Index(SlashDamageType), FixedPoint2.New(20));
		_damageable.TryChangeDamage(participant, damageSpec, ignoreResistances: false);
		
		// Apply heavy bleeding (5 units/second)
		if (TryComp<BloodstreamComponent>(participant, out var bloodstream))
		{
			_bloodstream.TryModifyBleedAmount((participant, bloodstream), 5.0f);
		}
		
		// Apply stun and knockdown
		_stun.TryAddParalyzeDuration(participant, TimeSpan.FromSeconds(3));
	}
		
		// Stun the victim as well, so they don't run away. 
		// This stun should apply even if they're waking up from nocturine.
		_stun.TryAddParalyzeDuration(victim, TimeSpan.FromSeconds(10));
		
	// NOW remove the mindshield from the victim (at the end of the ritual)
	// Note: We already validated the victim has a MindShieldComponent above, but double-check here for safety
	// The victim is guaranteed to be the original entity stored in ritual.Victim
	if (!TryComp<MindShieldComponent>(victim, out var _))
	{
		// No mindshield component - this should have been caught earlier, but handle it gracefully
		_popupSystem.PopupEntity(
			Loc.GetString("cult-invocation-fail"),
			user, user, PopupType.MediumCaution
		);
		args.Handled = true;
		return;
	}
	
	// Find the physical mindshield implant and destroy it
	if (_container.TryGetContainer(victim, ImplanterComponent.ImplantSlotId, out var implantContainer))
	{
		EntityUid? mindshieldImplant = null;
		
		// Find the mindshield implant by checking both tag AND SubdermalImplantComponent
		foreach (var implant in implantContainer.ContainedEntities)
		{
			if (_tag.HasTag(implant, MindShieldTag) && TryComp<SubdermalImplantComponent>(implant, out var _))
			{
				mindshieldImplant = implant;
				break;
			}
		}
		
		// If we found the implant, destroy it (this will automatically remove the MindShieldComponent)
		if (mindshieldImplant != null)
		{
			_implantSystem.ForceRemove(victim, mindshieldImplant.Value);
		}
		else
		{
			// No mindshield implant found - already removed somehow or mismatch
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail"),
				user, user, PopupType.MediumCaution
			);
			args.Handled = true;
			return;
		}
	}
	else
	{
		// No implant container - entity can't have implants
		_popupSystem.PopupEntity(
			Loc.GetString("cult-invocation-fail"),
			user, user, PopupType.MediumCaution
		);
		args.Handled = true;
		return;
	}
		
	// Show success message to all participants who were affected by the ritual
	foreach (var participant in validParticipants)
	{
		_popupSystem.PopupEntity(
			Loc.GetString("cult-invocation-mindshield-success"),
			participant, participant, PopupType.Large
		);
	}

	// After breaking the mindshield, attempt to convert the victim
	// Check if victim can now be converted (no longer has mindshield)
	if (_CanBeConverted(victim))
	{
		// Convert the victim using the same cultists who broke the mindshield
		_bloodCultist.UseConvertRune(victim, user, runeEntity, validParticipants.ToArray());
		
		// Add blood to the ritual pool based on the victim's current blood level
		// If they're at 50% blood, only add 50u instead of 100u
		// Also account for blood already spilled from EdgeEssentia wounds
		if (TryComp<BloodstreamComponent>(victim, out var victimBloodstream))
		{
			var bloodPercentage = _bloodstream.GetBloodLevel((victim, victimBloodstream));
			var bloodFromConversion = 100.0 * bloodPercentage;
			
			// Check if this entity has already contributed blood via EdgeEssentia bleeding
		var alreadyContributed = 0.0;
		if (TryComp<BloodCollectionTrackerComponent>(victim, out var tracker))
		{
			alreadyContributed = tracker.TotalBloodCollected;
		}
		
			// Ensure total contribution from this entity never exceeds 100 units
			var remainingAllowance = Math.Max(0, 100.0 - alreadyContributed);
			var bloodToAdd = Math.Min(bloodFromConversion, remainingAllowance);
			
			if (bloodToAdd > 0)
			{
				_bloodCultRule.AddBloodForConversion(bloodToAdd);
				
				// Update the tracker
				var conversionTracker = EnsureComp<BloodCollectionTrackerComponent>(victim);
				conversionTracker.TotalBloodCollected = Math.Min(conversionTracker.TotalBloodCollected + (float)bloodToAdd, conversionTracker.MaxBloodPerEntity);
				Dirty(victim, conversionTracker);
			}
		}
	}

	args.Handled = true;
	}

	// Disabled - cultists should revive and convert instead
	// May re-enable in the future if shells become more common
	// private void _SacrificeIntoShell(EntityUid victim, EntityUid user, EntityUid shell, EntityUid[] cultistsInRange)
	// {
	// 	// Get the victim's mind
	// 	EntityUid? mindId = CompOrNull<MindContainerComponent>(victim)?.Mind;
	// 	MindComponent? mindComp = CompOrNull<MindComponent>(mindId);
	// 	
	// 	if (mindId == null || mindComp == null)
	// 	{
	// 		_popupSystem.PopupEntity(
	// 			Loc.GetString("cult-invocation-fail-nosoul"),
	// 			user, user, PopupType.MediumCaution
	// 		);
	// 		return;
	// 	}
	//
	// 	// Check if enough cultists are present
	// 	// Require at least 1 cultist (matching the regular sacrifice requirement)
	// 	if (cultistsInRange.Length < 1)
	// 	{
	// 		_popupSystem.PopupEntity(
	// 			Loc.GetString("cult-invocation-fail"),
	// 			user, user, PopupType.MediumCaution
	// 		);
	// 		return;
	// 	}
	//
	// 	// Perform the sacrifice ritual announcement
	// 	foreach (EntityUid invoker in cultistsInRange)
	// 	{
	// 		// Make cultists speak the ritual words
	// 		// This mimics the behavior in BloodCultRuleSystem
	// 		if (TryComp<BloodCultistComponent>(invoker, out var _))
	// 		{
	// 			// Speak ritual words
	// 		}
	// 	}
	//
	// 	// Get shell coordinates
	// 	var shellCoordinates = Transform(shell).Coordinates;
	// 	
	// 	// Play sacrifice audio
	// 	_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/disintegrate.ogg"), shellCoordinates);
	// 	
	// 	// Delete the shell and spawn the juggernaut
	// 	QueueDel(shell);
	// 	var juggernaut = Spawn("MobBloodCultJuggernaut", shellCoordinates);
	// 	
	// 	// Get the juggernaut's body container
	// 	if (_container.TryGetContainer(juggernaut, "juggernaut_body_container", out var container))
	// 	{
	// 		// Insert the victim's body into the juggernaut
	// 		_container.Insert(victim, container);
	// 	}
	// 	
	// 	// Transfer mind from victim to juggernaut
	// 	_mind.TransferTo((EntityUid)mindId, juggernaut, mind:mindComp);
	// 	
	// 	// Play transformation audio
	// 	_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), shellCoordinates);
	// 	
	// 	// Notify the cultists
	// 	_popupSystem.PopupEntity(
	// 		Loc.GetString("cult-juggernaut-created"),
	// 		user, user, PopupType.Large
	// 	);
	// }
	}
}
