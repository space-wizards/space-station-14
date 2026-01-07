// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Content.Shared.Interaction.Events;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Server.GameTicking.Rules;
using Content.Shared.Mobs.Systems;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Mobs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Server.Roles;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Server.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.Movement.Events;
using Content.Shared.Speech;
using Content.Shared.Emoting;
using Content.Shared.Effects;
using System.Collections.Generic;

namespace Content.Server.BloodCult.EntitySystems;

public sealed class SoulStoneSystem : EntitySystem
{
	[Dependency] private readonly MindSystem _mind = default!;
	[Dependency] private readonly PopupSystem _popupSystem = default!;
	[Dependency] private readonly SharedAudioSystem _audioSystem = default!;
	[Dependency] private readonly BloodCultRuleSystem _cultRuleSystem = default!;
	[Dependency] private readonly BloodCultConstructSystem _constructSystem = default!;
	[Dependency] private readonly MobStateSystem _mobState = default!;
	[Dependency] private readonly IEntityManager _entityManager = default!;
	[Dependency] private readonly DamageableSystem _damageable = default!;
	[Dependency] private readonly RoleSystem _role = default!;
	[Dependency] private readonly BloodstreamSystem _bloodstream = default!;
	[Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

	private EntityQuery<ShadeComponent> _shadeQuery;

	public override void Initialize()
    {
        base.Initialize();

		SubscribeLocalEvent<SoulStoneComponent, AfterInteractEvent>(OnTryCaptureSoul);
		SubscribeLocalEvent<SoulStoneComponent, UseInHandEvent>(OnUseInHand);
		SubscribeLocalEvent<ShadeComponent, MobStateChangedEvent>(OnShadeDeath);
		SubscribeLocalEvent<SoulStoneComponent, DestructionEventArgs>(OnSoulStoneDestroyed);
		
		// Prevent soulstones from moving or rotating
		SubscribeLocalEvent<SoulStoneComponent, UpdateCanMoveEvent>(OnSoulstoneMove);
		SubscribeLocalEvent<SoulStoneComponent, MoveInputEvent>(OnSoulstoneMoveInput);
		
		// Ensure soulstones can speak and emote when they have a mind
		SubscribeLocalEvent<SoulStoneComponent, ComponentStartup>(OnSoulstoneStartup);
		SubscribeLocalEvent<SoulStoneComponent, DamageChangedEvent>(OnSoulstoneDamaged);

		_shadeQuery = GetEntityQuery<ShadeComponent>();
	}
	
	private void OnSoulstoneStartup(EntityUid uid, SoulStoneComponent component, ComponentStartup args)
	{
		// Ensure the soulstone has speech components if it has a mind
		if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.Mind != null)
		{
			EnsureComp<SpeechComponent>(uid);
			EnsureComp<EmotingComponent>(uid);
		}
	}

	private void OnSoulstoneMove(EntityUid uid, SoulStoneComponent component, UpdateCanMoveEvent args)
	{
		// Prevent all movement for soulstones
		// todo: Make it so they can't rotate when they click around on the ground.
		args.Cancel();
	}

	private void OnSoulstoneMoveInput(EntityUid uid, SoulStoneComponent component, ref MoveInputEvent args)
	{
		// Prevent rotation by not processing the input at all
		// The UpdateCanMoveEvent already prevents actual movement
	}

	private void OnSoulstoneDamaged(EntityUid uid, SoulStoneComponent component, DamageChangedEvent args)
	{
		if (!args.DamageIncreased)
			return;

		var xform = Transform(uid);
		var filter = Filter.Pvs(xform.Coordinates, entityMan: EntityManager);
		_color.RaiseEffect(Color.FromHex("#bf2cff"), new List<EntityUid> { uid }, filter);
	}

	private void OnTryCaptureSoul(Entity<SoulStoneComponent> ent, ref AfterInteractEvent args)
	{
		if (args.Handled
			|| !args.CanReach
			|| !args.ClickLocation.IsValid(_entityManager)
			|| !TryComp<BloodCultistComponent>(args.User, out var cultist) // ensure user is cultist
			|| HasComp<ActiveDoAfterComponent>(args.User)
			|| args.Target == null)
			return;

		if (!_shadeQuery.HasComponent(args.Target))
		{
			_constructSystem.TryApplySoulStone(ent, ref args);
			return;
		}

		args.Handled = true;

		if (args.Target != null && !_mobState.IsDead((EntityUid)args.Target) && TryComp<MindContainerComponent>(args.Target, out var mindContainer))
		{
			if (mindContainer.Mind != null)
			{
				var coordinates = Transform((EntityUid)args.Target).Coordinates;
				_mind.TransferTo((EntityUid)mindContainer.Mind, ent);
				_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), coordinates);
				_popupSystem.PopupEntity(
					Loc.GetString("cult-shade-recalled"),
					args.User, args.User, PopupType.SmallCaution
				);
				QueueDel(args.Target);
			}
		}
	}

	private void OnUseInHand(Entity<SoulStoneComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

		args.Handled = true;

		if (TryComp<BloodCultistComponent>(args.User, out var _) && TryComp<MindContainerComponent>(ent, out var mindContainer))
		{
			if (mindContainer.Mind != null && TryComp<MindComponent>((EntityUid)mindContainer.Mind, out var mindComp))
			{
				// Make the mind in the soulstone a cultist (without giving them cultist abilities yet)
				var mindId = (EntityUid)mindContainer.Mind;
				if (!_role.MindHasRole<BloodCultRoleComponent>(mindId))
				{
					_role.MindAddRole(mindId, "MindRoleCultist", mindComp);
				}
				
				// Damage the user for releasing the shade
				if (TryComp<DamageableComponent>(args.User, out var damageableForShade))
				{
					var damage = new DamageSpecifier();
					
					// Deal significant slash damage (30 points)
					damage.DamageDict.Add("Slash", FixedPoint2.New(30));
					
					_damageable.TryChangeDamage((args.User, damageableForShade), damage, ignoreResistances: false, interruptsDoAfters: true);
					
					// Add a very large bleed if the user has a bloodstream
					if (TryComp<BloodstreamComponent>(args.User, out var bloodstream))
					{
						// Add 10 units/second bleed - this is a massive bleed that will rapidly drain blood
						_bloodstream.TryModifyBleedAmount((args.User, bloodstream), 10.0f);
					}
				}
				
				var summonCoordinates = Transform((EntityUid)args.User).Coordinates;
				var shadeEntity = Spawn("MobBloodCultShade", summonCoordinates);
				_mind.TransferTo(mindId, shadeEntity);
				
				// Set the soulstone reference on the Shade so it knows where to return
				if (TryComp<ShadeComponent>(shadeEntity, out var shadeComponent))
				{
					shadeComponent.SourceSoulstone = ent;
				}
			
				_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), summonCoordinates);
				_popupSystem.PopupEntity(
					Loc.GetString("cult-shade-summoned"),
					args.User, args.User, PopupType.SmallCaution
				);
				var summonerDisplayName = _entityManager.GetComponent<MetaDataComponent>(args.User).EntityName;
				_cultRuleSystem.AnnounceToCultist(Loc.GetString("cult-shade-servant", ("name", summonerDisplayName)), shadeEntity);
			}
			else
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-soulstone-empty"),
					args.User, args.User, PopupType.SmallCaution
				);
			}
		}
	}

	private void OnShadeDeath(Entity<ShadeComponent> shade, ref MobStateChangedEvent args)
	{
		// Only handle when the Shade dies
		if (args.NewMobState != MobState.Dead)
			return;

		// Check if the Shade has a source soulstone and a mind
		if (shade.Comp.SourceSoulstone == null)
			return;


		var soulstone = shade.Comp.SourceSoulstone.Value;
		
		// Verify the soulstone still exists
		if (!Exists(soulstone))
			return;

		// Get the Shade's mind
		EntityUid? mindId = CompOrNull<MindContainerComponent>(shade)?.Mind;
		if (mindId == null || !TryComp<MindComponent>(mindId, out var mindComp))
			return;

	// Transfer the mind back to the soulstone
	var coordinates = Transform(shade).Coordinates;
	_mind.TransferTo(mindId.Value, soulstone);
	
	// Ensure the soulstone can speak but not move
	EnsureComp<SpeechComponent>(soulstone);
	EnsureComp<EmotingComponent>(soulstone);
	
	_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), coordinates);
		
		// Delete the Shade entity
		QueueDel(shade);
	}

	private void OnSoulStoneDestroyed(Entity<SoulStoneComponent> soulstone, ref DestructionEventArgs args)
	{
	
		// Figure out where the soulstone is
		var coordinates = Transform(soulstone).Coordinates;
		// Glassbreak sound playing at the coordinates above
		_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Effects/glass_break1.ogg"), coordinates);

		// Get the mind from the soulstone
		EntityUid? mindId = CompOrNull<MindContainerComponent>(soulstone)?.Mind;

		// Clear BloodCult antag role if present (e.g., from juggernauts)
		if (mindId != null && _role.MindHasRole<BloodCultRoleComponent>(mindId.Value))
		{
			if (TryComp<MindComponent>(mindId.Value, out var mindComp))
				_role.MindRemoveRole<BloodCultRoleComponent>((mindId.Value, mindComp));
		}

		// Figure out what the original entity was, probably a positronic brain or IPC brain
		if (soulstone.Comp.OriginalEntityPrototype != null)
		{
			var originalPrototype = soulstone.Comp.OriginalEntityPrototype.Value;
			// Spawn the original entity at the soulstone's location
			var originalEntity = Spawn(originalPrototype, coordinates);
			// Transfer the mind to the original entity
			if (mindId != null)
			{
				_mind.TransferTo(mindId.Value, originalEntity);
			}
		}


		_popupSystem.PopupEntity(
			Loc.GetString("cult-soulstone-shattered"),
			soulstone, PopupType.MediumCaution
		);
	}
}

