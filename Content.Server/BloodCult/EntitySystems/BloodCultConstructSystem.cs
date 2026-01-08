// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2026 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameObjects;
using Content.Server.Mind;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Server.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Content.Shared.Damage;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Shared.Speech;
using Content.Shared.Emoting;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;
using Content.Shared.Damage.Components;

namespace Content.Server.BloodCult.EntitySystems;

public sealed partial class BloodCultConstructSystem : EntitySystem
{
	[Dependency] private readonly MindSystem _mind = default!;
	[Dependency] private readonly MobStateSystem _mobState = default!;
	[Dependency] private readonly PopupSystem _popup = default!;
	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly SharedContainerSystem _container = default!;
	[Dependency] private readonly SharedPhysicsSystem _physics = default!;
	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly SharedTransformSystem _transform = default!;
	[Dependency] private readonly NpcFactionSystem _npcFaction = default!;
	[Dependency] private readonly SharedActionsSystem _actions = default!;
	[Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

	/// <summary>
	/// Grants the Commune action to a juggernaut
	/// </summary>
	private void GrantCommuneAction(EntityUid juggernaut)
	{
		EntityUid? communeAction = null;
		ActionComponent? actionComp = null;
		if (_actions.AddAction(juggernaut, ref communeAction, out actionComp, (ProtoId<EntityPrototype>)"ActionCultistCommune") && communeAction != null && actionComp != null)
		{
			// Ensure the event is raised on the juggernaut so it can be handled
			// Note: RaiseOnUser property might not exist in current API, check if needed
			// actionComp.RaiseOnUser = true;
			Dirty(communeAction.Value, actionComp);
		}
	}


	public override void Initialize()
	{
		base.Initialize();
		
		// CanDropTargetEvent is handled in SharedBloodCultistSystem for both client and server
		SubscribeLocalEvent<BloodCultConstructShellComponent, DragDropTargetEvent>(OnDragDropTarget);
		SubscribeLocalEvent<JuggernautComponent, MobStateChangedEvent>(OnJuggernautStateChanged);
		SubscribeLocalEvent<JuggernautComponent, DragDropTargetEvent>(OnJuggernautDragDropTarget);
		
		// Remove StaminaComponent from any existing juggernauts (in case they were spawned before this system was added)
		// Juggernauts can't be stunned, so stamina damage is meaningless
		var query = AllEntityQuery<JuggernautComponent, StaminaComponent>();
		while (query.MoveNext(out var uid, out _, out _))
		{
			RemComp<StaminaComponent>(uid);
		}
		
		// Handle alt-fire (right-click) attack to find nearest enemy for juggernauts and shades
		// With AltDisarm = false, right-clicking sends HeavyAttackEvent instead of DisarmAttackEvent
		SubscribeNetworkEvent<HeavyAttackEvent>(OnHeavyAttack, before: new[] { typeof(SharedMeleeWeaponSystem) });
	}

	public void TryApplySoulStone(Entity<SoulStoneComponent> ent, ref AfterInteractEvent args)
    {
		if (args.Target == null)
			return;

		// Check if target is a juggernaut shell
		if (HasComp<BloodCultConstructShellComponent>(args.Target))
		{
			_ActivateJuggernautShell(ent, args.User, args.Target.Value);
			args.Handled = true;
			return;
		}

		// Check if target is an inactive juggernaut (critical state)
		if (TryComp<JuggernautComponent>(args.Target, out var juggComp) && juggComp.IsInactive)
		{
			_ReactivateJuggernaut(ent, args.User, args.Target.Value, juggComp);
			args.Handled = true;
			return;
		}
	}

	private void _ActivateJuggernautShell(EntityUid soulstone, EntityUid user, EntityUid shell)
	{
		// Get the mind from the soulstone
		EntityUid? mindId = CompOrNull<MindContainerComponent>(soulstone)?.Mind;
		MindComponent? mindComp = CompOrNull<MindComponent>(mindId);
		
		if (mindId == null || mindComp == null)
		{
			//No mind in the soulstone
			_popup.PopupEntity(Loc.GetString("cult-soulstone-empty"), user, user, PopupType.Medium);
			return;
		}
		
		// Figure out the shell's location so we can spawn the completed juggernaut there
		var shellTransform = Transform(shell);
		var shellMapCoords = _transform.GetMapCoordinates(shellTransform);
		var shellRotation = shellTransform.LocalRotation;
		
		// Unanchor the shell first to prevent grid snapping issues
		if (shellTransform.Anchored)
		{
			_transform.Unanchor(shell, shellTransform);
		}
		
		// Play sacrifice audio
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/disintegrate.ogg"), shellTransform.Coordinates);
		
		// Delete the shell and spawn the juggernaut at the exact map coordinates with rotation
		// Use DeleteEntity instead of QueueDel to ensure immediate deletion before spawning
		EntityManager.DeleteEntity(shell);
		
		// Spawn the juggernaut at the exact map coordinates (not anchored, so it won't snap to grid)
		var juggernaut = Spawn("MobBloodCultJuggernaut", shellMapCoords, rotation: shellRotation);
		
		// Remove StaminaComponent - juggernauts can't be stunned so stamina damage is meaningless
		RemComp<StaminaComponent>(juggernaut);
		
		// Ensure the juggernaut is not anchored (mobs shouldn't be anchored)
		var juggernautTransform = Transform(juggernaut);
		if (juggernautTransform.Anchored)
		{
			_transform.Unanchor(juggernaut, juggernautTransform);
		}
		
		// Store the soulstone in the juggernaut's container. It'll be ejected if the juggernaut is crit
		if (_container.TryGetContainer(juggernaut, "juggernaut_soulstone_container", out var soulstoneContainer))
		{
			_container.Insert(soulstone, soulstoneContainer);
		}
		
		// Store reference to soulstone in the juggernaut component and set as active
		if (TryComp<JuggernautComponent>(juggernaut, out var juggComp))
		{
			juggComp.SourceSoulstone = soulstone;
			juggComp.IsInactive = false;
		}
		
		// Transfer mind from soulstone to juggernaut
		_mind.TransferTo((EntityUid)mindId, juggernaut, mind:mindComp);
		
		// Preserve speech component from soulstone only if it's a Hamlet soulstone
		if (TryComp<SoulStoneComponent>(soulstone, out var soulstoneComp) && 
		    soulstoneComp.OriginalEntityPrototype == "MobHamsterHamlet" &&
		    TryComp<SpeechComponent>(soulstone, out var soulstoneSpeech))
		{
			// Remove existing speech component if present, then copy from soulstone
			if (HasComp<SpeechComponent>(juggernaut))
				RemComp<SpeechComponent>(juggernaut);
			CopyComp(soulstone, juggernaut, soulstoneSpeech);
		}
		
		// Ensure juggernaut is in the BloodCultist faction (remove any crew alignment)
		// Use ClearFactions and AddFaction to ensure proper faction alignment after mind transfer
		if (TryComp<NpcFactionMemberComponent>(juggernaut, out var npcFaction))
		{
			_npcFaction.ClearFactions((juggernaut, npcFaction), false);
		}
		_npcFaction.AddFaction(juggernaut, BloodCultRuleSystem.BloodCultistFactionId);
		
		// Grant Commune ability to juggernaut
		GrantCommuneAction(juggernaut);
		
		// Play transformation audio
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), shellTransform.Coordinates);
		
		// Play a message
		_popup.PopupEntity(Loc.GetString("cult-juggernaut-created"), user, user, PopupType.Large);
	}

	private void _ReactivateJuggernaut(EntityUid soulstone, EntityUid user, EntityUid juggernaut, JuggernautComponent juggComp)
	{
		// Get the mind from the soulstone
		EntityUid? mindId = CompOrNull<MindContainerComponent>(soulstone)?.Mind;
		MindComponent? mindComp = CompOrNull<MindComponent>(mindId);
		
		if (mindId == null || mindComp == null)
		{
			_popup.PopupEntity(Loc.GetString("cult-soulstone-empty"), user, user, PopupType.Medium);
			return;
		}

		// Store the soulstone in the juggernaut's container
		if (_container.TryGetContainer(juggernaut, "juggernaut_soulstone_container", out var soulstoneContainer))
		{
			_container.Insert(soulstone, soulstoneContainer);
		}

		// Store reference to soulstone and reactivate the juggernaut
		juggComp.SourceSoulstone = soulstone;
		juggComp.IsInactive = false;

		// Grant Commune ability to juggernaut if not already granted
		GrantCommuneAction(juggernaut);

		// DON'T heal the juggernaut - it stays in critical state until healed with blood

		// Transfer mind from soulstone to juggernaut
		_mind.TransferTo((EntityUid)mindId, juggernaut, mind: mindComp);
		
		// Preserve speech component from soulstone (e.g., Hamlet's squeak sounds)
		if (TryComp<SpeechComponent>(soulstone, out var soulstoneSpeech))
		{
			// Remove existing speech component if present, then copy from soulstone
			if (HasComp<SpeechComponent>(juggernaut))
				RemComp<SpeechComponent>(juggernaut);
			CopyComp(soulstone, juggernaut, soulstoneSpeech);
		}
		
		// Ensure juggernaut is in the BloodCultist faction (remove any crew alignment)
		// Use ClearFactions and AddFaction to ensure proper faction alignment after mind transfer
		if (TryComp<NpcFactionMemberComponent>(juggernaut, out var npcFaction))
		{
			_npcFaction.ClearFactions((juggernaut, npcFaction), false);
		}
		_npcFaction.AddFaction(juggernaut, BloodCultRuleSystem.BloodCultistFactionId);

		// Play transformation audio
		var coordinates = Transform(juggernaut).Coordinates;
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), coordinates);

		// Notify the user
		_popup.PopupEntity(Loc.GetString("cult-juggernaut-reactivated"), user, user, PopupType.Large);
	}

	// Handle dragging dead bodies onto the juggernaut shell to create a juggernaut
	private void OnDragDropTarget(EntityUid uid, BloodCultConstructShellComponent component, ref DragDropTargetEvent args)
	{
		// Mark as handled immediately to prevent other systems from processing this
		args.Handled = true;
		
		// Verify the dragged entity is a dead body with a mind
		if (!_mobState.IsDead(args.Dragged))
		{
			_popup.PopupEntity(Loc.GetString("cult-juggernaut-shell-needs-dead"), args.User, args.User, PopupType.Medium);
			return;
		}

		EntityUid? mindId = CompOrNull<MindContainerComponent>(args.Dragged)?.Mind;
		MindComponent? mindComp = CompOrNull<MindComponent>(mindId);
		
		if (mindId == null || mindComp == null)
		{
			_popup.PopupEntity(Loc.GetString("cult-invocation-fail-nosoul"), args.User, args.User, PopupType.Medium);
			return;
		}

		var shellTransform = Transform(uid);
		var shellMapCoords = _transform.GetMapCoordinates(shellTransform);
		var shellRotation = shellTransform.LocalRotation;
		
		// Unanchor the shell first to prevent grid snapping issues
		if (shellTransform.Anchored)
		{
			_transform.Unanchor(uid, shellTransform);
		}
		
		// Spawn the juggernaut BEFORE deleting the shell to ensure proper setup
		// Spawn at the exact map coordinates (not anchored, so it won't snap to grid)
		var juggernaut = Spawn("MobBloodCultJuggernaut", shellMapCoords, rotation: shellRotation);
		
		// Remove StaminaComponent - juggernauts can't be stunned so stamina damage is meaningless
		RemComp<StaminaComponent>(juggernaut);
		
		// Ensure the juggernaut is not anchored (mobs shouldn't be anchored)
		var juggernautTransform = Transform(juggernaut);
		if (juggernautTransform.Anchored)
		{
			_transform.Unanchor(juggernaut, juggernautTransform);
		}
		
		// Get the juggernaut's body container and insert the body BEFORE deleting the shell
		// This prevents the body from being detected by offering runes underneath
		if (_container.TryGetContainer(juggernaut, "juggernaut_body_container", out var container))
		{
			// Insert the victim's body into the juggernaut
			_container.Insert(args.Dragged, container);
		}
		
		// Play sacrifice audio
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/disintegrate.ogg"), shellTransform.Coordinates);
		
		// Delete the shell AFTER the body is safely in the container
		// Use DeleteEntity instead of QueueDel to ensure immediate deletion
		EntityManager.DeleteEntity(uid);
		
		// Store reference to body in the juggernaut component
		if (TryComp<JuggernautComponent>(juggernaut, out var juggComp))
		{
			juggComp.SourceBody = args.Dragged;
			juggComp.IsInactive = false;
		}
		
		// Transfer mind from victim to juggernaut
		_mind.TransferTo((EntityUid)mindId, juggernaut, mind:mindComp);
		
		// Ensure juggernaut is in the BloodCultist faction (remove any crew alignment)
		// Use ClearFactions and AddFaction to ensure proper faction alignment after mind transfer
		if (TryComp<NpcFactionMemberComponent>(juggernaut, out var npcFaction))
		{
			_npcFaction.ClearFactions((juggernaut, npcFaction), false);
		}
		_npcFaction.AddFaction(juggernaut, BloodCultRuleSystem.BloodCultistFactionId);
		
		// Grant Commune ability to juggernaut
		GrantCommuneAction(juggernaut);
		
		// Play transformation audio
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), shellTransform.Coordinates);
		
		// Notify the user
		_popup.PopupEntity(Loc.GetString("cult-juggernaut-created"), args.User, args.User, PopupType.Large);
		
		args.Handled = true;
	}


	private void OnJuggernautStateChanged(Entity<JuggernautComponent> juggernaut, ref MobStateChangedEvent args)
	{
		// Handle transition to critical state or death
		if (args.NewMobState != MobState.Critical && args.NewMobState != MobState.Dead)
			return;

		// Don't eject if already inactive
		if (juggernaut.Comp.IsInactive)
			return;

		// Get the juggernaut's mind
		EntityUid? mindId = CompOrNull<MindContainerComponent>(juggernaut)?.Mind;
		if (mindId == null || !TryComp<MindComponent>(mindId, out var mindComp))
			return;

		bool ejectedSomething = false;

		// Handle soulstone ejection
		if (juggernaut.Comp.SourceSoulstone != null)
		{
			var soulstone = juggernaut.Comp.SourceSoulstone.Value;

			// Verify the soulstone still exists
			if (Exists(soulstone))
			{
				// Transfer the mind back to the soulstone
				_mind.TransferTo((EntityUid)mindId, soulstone, mind: mindComp);
				
				// Ensure the soulstone can speak but not move
				EnsureComp<SpeechComponent>(soulstone);
				EnsureComp<EmotingComponent>(soulstone);

				// Remove the soulstone from the container
				if (_container.TryGetContainer(juggernaut, "juggernaut_soulstone_container", out var soulstoneContainer))
				{
					_container.Remove(soulstone, soulstoneContainer);
				}

				// Give the soulstone a physics push for visual effect
				if (TryComp<PhysicsComponent>(soulstone, out var soulstonePhysics))
				{
					_physics.SetAwake((soulstone, soulstonePhysics), true);
					var randomDirection = _random.NextVector2();
					var speed = _random.NextFloat(8f, 15f);
					var impulse = randomDirection * speed * soulstonePhysics.Mass;
					_physics.ApplyLinearImpulse(soulstone, impulse, body: soulstonePhysics);
				}

				// Show popup
				_popup.PopupEntity(
					Loc.GetString("cult-juggernaut-critical-soulstone-ejected"),
					juggernaut, PopupType.LargeCaution
				);

				juggernaut.Comp.SourceSoulstone = null;
				ejectedSomething = true;
			}
		}

		// Handle body ejection
		if (juggernaut.Comp.SourceBody != null)
		{
			var body = juggernaut.Comp.SourceBody.Value;

			// Verify the body still exists
			if (Exists(body))
			{
				// Transfer the mind back to the body
				_mind.TransferTo((EntityUid)mindId, body, mind: mindComp);

				// Remove the body from the container
				if (_container.TryGetContainer(juggernaut, "juggernaut_body_container", out var bodyContainer))
				{
					_container.Remove(body, bodyContainer);
				}

				// Give the body a physics push for visual effect
				if (TryComp<PhysicsComponent>(body, out var bodyPhysics))
				{
					_physics.SetAwake((body, bodyPhysics), true);
					var randomDirection = _random.NextVector2();
					var speed = _random.NextFloat(8f, 15f);
					var impulse = randomDirection * speed * bodyPhysics.Mass;
					_physics.ApplyLinearImpulse(body, impulse, body: bodyPhysics);
				}

				// Show popup
				_popup.PopupEntity(
					Loc.GetString("cult-juggernaut-critical-soulstone-ejected"), // Reuse existing string for now
					juggernaut, PopupType.LargeCaution
				);

				juggernaut.Comp.SourceBody = null;
				ejectedSomething = true;
			}
		}

		// Play audio effect and mark inactive if we ejected something
		if (ejectedSomething)
		{
			var coordinates = Transform(juggernaut).Coordinates;
			_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), coordinates);
			juggernaut.Comp.IsInactive = true;
		}
	}

	private void OnJuggernautDragDropTarget(EntityUid uid, JuggernautComponent component, ref DragDropTargetEvent args)
	{
		// Only allow reactivating inactive juggernauts
		if (!component.IsInactive)
		{
			args.Handled = true;
			return;
		}

		// Verify the dragged entity is a dead body with a mind
		if (!_mobState.IsDead(args.Dragged))
		{
			_popup.PopupEntity(Loc.GetString("cult-juggernaut-shell-needs-dead"), args.User, args.User, PopupType.Medium);
			args.Handled = true;
			return;
		}

		EntityUid? mindId = CompOrNull<MindContainerComponent>(args.Dragged)?.Mind;
		MindComponent? mindComp = CompOrNull<MindComponent>(mindId);
		
		if (mindId == null || mindComp == null)
		{
			_popup.PopupEntity(Loc.GetString("cult-invocation-fail-nosoul"), args.User, args.User, PopupType.Medium);
			args.Handled = true;
			return;
		}

		_ReactivateJuggernautWithBody(args.Dragged, args.User, uid, component);
		args.Handled = true;
	}

	private void _ReactivateJuggernautWithBody(EntityUid body, EntityUid user, EntityUid juggernaut, JuggernautComponent juggComp)
	{
		// Get the mind from the body
		EntityUid? mindId = CompOrNull<MindContainerComponent>(body)?.Mind;
		MindComponent? mindComp = CompOrNull<MindComponent>(mindId);
		
		if (mindId == null || mindComp == null)
		{
			_popup.PopupEntity(Loc.GetString("cult-invocation-fail-nosoul"), user, user, PopupType.Medium);
			return;
		}

		// Store the body in the juggernaut's container
		if (_container.TryGetContainer(juggernaut, "juggernaut_body_container", out var bodyContainer))
		{
			_container.Insert(body, bodyContainer);
		}

		// Store reference to body and reactivate the juggernaut
		juggComp.SourceBody = body;
		juggComp.IsInactive = false;

		// Grant Commune ability to juggernaut if not already granted
		GrantCommuneAction(juggernaut);

		// DON'T heal the juggernaut - it stays in critical state until healed with blood

		// Transfer mind from body to juggernaut
		_mind.TransferTo((EntityUid)mindId, juggernaut, mind: mindComp);
		
		// Ensure juggernaut is in the BloodCultist faction (remove any crew alignment)
		if (TryComp<NpcFactionMemberComponent>(juggernaut, out var npcFaction))
		{
			_npcFaction.ClearFactions((juggernaut, npcFaction), false);
		}
		_npcFaction.AddFaction(juggernaut, BloodCultRuleSystem.BloodCultistFactionId);

		// Play transformation audio
		var coordinates = Transform(juggernaut).Coordinates;
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), coordinates);

		// Notify the user
		_popup.PopupEntity(Loc.GetString("cult-juggernaut-reactivated"), user, user, PopupType.Large);
	}

	/// <summary>
	/// Handles alt-fire (right-click) attacks for juggernauts and shades.
	/// When right-clicking without a target, finds the nearest hostile enemy and performs a light attack.
	/// Similar to how zombies bite the nearest enemy on right-click.
	/// </summary>
	private void OnHeavyAttack(HeavyAttackEvent ev, EntitySessionEventArgs args)
	{
		if (args.SenderSession.AttachedEntity is not { } user)
			return;

		// Only handle for juggernauts and shades
		if (!HasComp<JuggernautComponent>(user) && !HasComp<ShadeComponent>(user))
			return;

		// Only handle if there's no specific target (right-click without clicking on an entity)
		// HeavyAttackEvent.Entities contains the entities the client thinks it hit.
		// If there are valid targets (not the user, damageable), let the normal heavy attack handle it.
		// Otherwise, we'll find the nearest enemy below.
		if (ev.Entities != null && ev.Entities.Count > 0)
		{
			foreach (var netEntity in ev.Entities)
			{
				if (TryGetEntity(netEntity, out var entity) && entity != user && HasComp<DamageableComponent>(entity))
				{
					// Valid target exists - let the normal heavy attack system handle this event
					return;
				}
			}
		}

		// No valid target found (or Entities was null/empty) - find the nearest enemy and attack them

		// Get the weapon (should be the entity itself for unarmed attacks)
		if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon))
			return;

		// Get the melee range
		var range = weapon.Range;

		// Find nearest hostile enemy within range
		EntityUid? nearestEnemy = null;
		float nearestDistance = float.MaxValue;

		var userXform = Transform(user);
		var userPos = _transform.GetWorldPosition(userXform);

		// Get nearby hostiles using faction system
		if (TryComp<NpcFactionMemberComponent>(user, out var factionComp))
		{
			foreach (var hostile in _npcFaction.GetNearbyHostiles((user, factionComp, null), range))
			{
				if (!TryComp<DamageableComponent>(hostile, out _))
					continue;

				var hostilePos = _transform.GetWorldPosition(hostile);
				var distance = (hostilePos - userPos).LengthSquared();

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearestEnemy = hostile;
				}
			}
		}

		// If we found a nearest enemy, perform a light attack on them
		if (nearestEnemy != null && nearestEnemy.Value.IsValid())
		{
			_melee.AttemptLightAttack(user, weaponUid, weapon, nearestEnemy.Value);
		}
	}
}
