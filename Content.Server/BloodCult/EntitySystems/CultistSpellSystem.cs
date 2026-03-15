using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Server.Power.Components;
using Content.Shared.FixedPoint;
using Content.Server.Popups;
using Content.Server.Hands.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Stacks;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Prototypes;
using Content.Shared.BloodCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Server.GameTicking.Rules;
using Content.Shared.Stunnable;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mind.Components;
using Content.Server.Stack;
using Content.Server.Mind;


namespace Content.Server.BloodCult.EntitySystems;

public sealed partial class CultistSpellSystem : EntitySystem
{
	private static readonly ProtoId<StackPrototype> RunedSteelStack = "RunedSteel";
	private static readonly ProtoId<StackPrototype> RunedGlassStack = "RunedGlass";
	private static readonly ProtoId<StackPrototype> RunedPlasteelStack = "RunedPlasteel";

	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly IPrototypeManager _proto = default!;
	[Dependency] private readonly SharedActionsSystem _action = default!;
	[Dependency] private readonly ActionContainerSystem _actionContainer = default!;
	[Dependency] private readonly MindSystem _mind = default!;
	[Dependency] private readonly DamageableSystem _damageableSystem = default!;
	[Dependency] private readonly PopupSystem _popup = default!;
	[Dependency] private readonly SharedAudioSystem _audioSystem = default!;
	[Dependency] private readonly BloodCultRuleSystem _bloodCultRules = default!;
	[Dependency] private readonly HandsSystem _hands = default!;
	//[Dependency] private readonly StaminaSystem _stamina = default!;
	[Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
	//[Dependency] private readonly IEntityManager _entMan = default!;
	[Dependency] private readonly SharedStunSystem _stun = default!;
	//[Dependency] private readonly ConstructionSystem _construction = default!;
	[Dependency] private readonly SharedStackSystem _stack = default!;
	[Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

	private static readonly ProtoId<DamageTypePrototype> BloodlossDamageType = "Bloodloss";
	private static readonly ProtoId<DamageTypePrototype> ShockDamageType = "Shock";
	private static readonly ProtoId<DamageTypePrototype> SlashDamageType = "Slash";

	//When runes are re-added, uncomment this
	//private EntityQuery<EmpowerOnStandComponent> _runeQuery; 

	private static string[] AvailableDaggers = ["CultDaggerCurved", "CultDaggerSerrated", "CultDaggerStraight"];

	public override void Initialize()
	{
		base.Initialize();

		//When runes are re-added, uncomment this
		//_runeQuery = GetEntityQuery<EmpowerOnStandComponent>();
		SubscribeLocalEvent<BloodCultistComponent, SpellsMessage>(OnSpellSelectedMessage);
		SubscribeLocalEvent<BloodCultistComponent, EventCultistSummonDagger>(OnSummonDagger);
		SubscribeLocalEvent<BloodCultistComponent, EventCultistStudyVeil>(OnStudyVeil);
		SubscribeLocalEvent<BloodCultistComponent, BloodCultCommuneSendMessage>(OnCommune);
		//When juggernauts are added, uncomment this
		//SubscribeLocalEvent<JuggernautComponent, BloodCultCommuneSendMessage>(OnJuggernautCommune);
		SubscribeLocalEvent<BloodCultistComponent, EventCultistSanguineDream>(OnSanguineDream);
		SubscribeLocalEvent<BloodCultistComponent, EventCultistTwistedConstruction>(OnTwistedConstruction);
		SubscribeLocalEvent<BloodCultistComponent, CarveSpellDoAfterEvent>(OnCarveSpellDoAfter);
	}

	private bool TryUseAbility(Entity<BloodCultistComponent> ent, BaseActionEvent args)
	{
		if (args.Handled)
            return false;
		if (!TryComp<CultistSpellComponent>(args.Action, out var actionComp))
            return false;

		// check if enough charges remain
		if (!actionComp.Infinite)
			actionComp.Charges = actionComp.Charges - 1;

		if (actionComp.Charges == 0)
		{
			_action.RemoveAction(args.Action.Owner);
			RemoveSpell(GetSpell(actionComp.AbilityId), ent.Comp);
		}

		// apply damage
		if (actionComp.HealthCost > 0 && TryComp<DamageableComponent>(ent, out var damageable))
		{
			DamageSpecifier appliedDamageSpecifier;
			if (damageable.Damage.DamageDict.ContainsKey("Bloodloss"))
				appliedDamageSpecifier = new DamageSpecifier(_proto.Index(BloodlossDamageType), FixedPoint2.New(actionComp.HealthCost));
			else if (damageable.Damage.DamageDict.ContainsKey("Shock"))
				appliedDamageSpecifier = new DamageSpecifier(_proto.Index(ShockDamageType), FixedPoint2.New(actionComp.HealthCost));
			else
				appliedDamageSpecifier = new DamageSpecifier(_proto.Index(SlashDamageType), FixedPoint2.New(actionComp.HealthCost));
			_damageableSystem.TryChangeDamage((ent, damageable), appliedDamageSpecifier, true, origin: ent);
		}

		// verbalize invocation - generate random 2-word chant (skip for StudyVeil and Commune abilities)
		if (actionComp.AbilityId != "StudyVeil" && actionComp.AbilityId != "Commune")
		{
			var invocation = _bloodCultRules.GenerateChant(wordCount: 2);
			_bloodCultRules.Speak(ent, invocation);
		}

		// play sound 
		if (actionComp.CastSound != null)
			_audioSystem.PlayPredicted(actionComp.CastSound, ent, ent);

		return true;
	}

	public CultAbilityPrototype GetSpell(ProtoId<CultAbilityPrototype> id)
		=> _proto.Index(id);

	public void AddSpell(EntityUid uid, BloodCultistComponent comp, ProtoId<CultAbilityPrototype> id, bool recordKnownSpell = true)
	{
		var data = GetSpell(id);

		//When runes are re-added, uncomment this
		//bool standingOnRune = IsStandingOnEmpoweringRune(uid);

		if (comp.KnownSpells.Count > 3)
		{
			_popup.PopupEntity(Loc.GetString("cult-spell-exceeded"), uid, uid);
			return;
		}

		// If not on an empowering rune and they have existing spells, remove the actions matching those spells
		//When runes are re-added, uncomment this
		//if (!standingOnRune && comp.KnownSpells.Count > 0)
		if (comp.KnownSpells.Count > 0) //Swap for the above when runes are re-added
		{
			RemoveActionsMatchingKnownSpells(uid, comp);
			// Clear KnownSpells since they can only have 0 spells when not on rune
			comp.KnownSpells.Clear();
			Dirty(uid, comp);
		}

        if (data.Event != null)
            RaiseLocalEvent(uid, (object) data.Event, true);

		if (data.ActionPrototypes == null || data.ActionPrototypes.Count <= 0)
			return;

		if (data.DoAfterLength > 0)
		{
			//Code for when runes are added
			//_popup.PopupEntity(standingOnRune ? Loc.GetString("cult-spell-carving-rune") : Loc.GetString("cult-spell-carving"), uid, uid, PopupType.MediumCaution);
			//var dargs = new DoAfterArgs(EntityManager, uid, data.DoAfterLength * (standingOnRune ? 1 : 3), new CarveSpellDoAfterEvent(
			//	uid, data, recordKnownSpell, standingOnRune), uid
			_popup.PopupEntity(Loc.GetString("cult-spell-carving"), uid, uid, PopupType.MediumCaution);
			var dargs = new DoAfterArgs(EntityManager, uid, data.DoAfterLength, new CarveSpellDoAfterEvent(
				GetNetEntity(uid), id, recordKnownSpell), uid //Placeholder code to make it work with no runes
			)
			{
				BreakOnDamage = true,
				RequireCanInteract = false,  // Allow restrained cultists to prepare spells
				NeedHand = false,  // Cultists don't need hands to prep spells
				BreakOnHandChange = false,
				BreakOnMove = true,
				BreakOnDropItem = false,
				CancelDuplicate = false,
			};

			_doAfter.TryStartDoAfter(dargs);
		}
		else
		{
			// Add actions to the mind's action container if available, otherwise to the entity
			if (_mind.TryGetMind(uid, out var mindId, out _))
			{
				foreach (var act in data.ActionPrototypes)
					_actionContainer.AddAction(mindId, act);
			}
			else
			{
				foreach (var act in data.ActionPrototypes)
					_action.AddAction(uid, act);
			}
			if (recordKnownSpell)
				comp.KnownSpells.Add(data);
		}
	}

	public void OnCarveSpellDoAfter(Entity<BloodCultistComponent> ent, ref CarveSpellDoAfterEvent args)
	{
		var cultAbility = _proto.Index(args.CultAbilityId);
		var carverUid = GetEntity(args.NetCarverUid);

		//Code for when runes are added
		//if (ent.Comp.KnownSpells.Count > 3 || (!args.StandingOnRune && ent.Comp.KnownSpells.Count > 0))
		if (ent.Comp.KnownSpells.Count > 3 || ent.Comp.KnownSpells.Count > 0)
		{
			_popup.PopupEntity(Loc.GetString("cult-spell-exceeded"), ent, ent);
			return;
		}
		if (cultAbility.ActionPrototypes == null)
			return;

		DamageSpecifier appliedDamageSpecifier = new DamageSpecifier(
			_proto.Index(SlashDamageType),
			//Code for when runes are added
			//FixedPoint2.New(cultAbility.HealthDrain * (args.StandingOnRune ? 1 : 3))
			FixedPoint2.New(cultAbility.HealthDrain)
		);

        if (!args.Cancelled)
		{
			// Add actions to the mind's action container if available, otherwise to the entity
			if (_mind.TryGetMind(carverUid, out var mindId, out _))
			{
				foreach (var act in cultAbility.ActionPrototypes)
				{
					_actionContainer.AddAction(mindId, act);
				}
			}
			else
			{
				foreach (var act in cultAbility.ActionPrototypes)
				{
					_action.AddAction(carverUid, act);
				}
			}
			if (args.RecordKnownSpell)
				ent.Comp.KnownSpells.Add(args.CultAbilityId);
			
			// Apply damage if health drain > 0
			if (cultAbility.HealthDrain > 0 && TryComp<DamageableComponent>(ent, out var damageableForDamage))
			{
				_damageableSystem.TryChangeDamage((ent, damageableForDamage), appliedDamageSpecifier, true, origin: ent);
			}
			_audioSystem.PlayPvs(cultAbility.CarveSound, ent);
		/* Rune logic
		if (args.StandingOnRune)
		{
			// Generate random chant when empowered by rune
			var invocation = _bloodCultRules.GenerateChant(wordCount: 2);
			_bloodCultRules.Speak(ent, invocation);
		}
		*/
		}

        Dirty(ent, ent.Comp);
	}

	public void RemoveSpell(ProtoId<CultAbilityPrototype> id, BloodCultistComponent comp)
	{
		comp.KnownSpells.Remove(GetSpell(id));
	}


	/// <summary>
	/// Removes actions that match spells in the cultist's KnownSpells list.
	/// This is called when adding a new spell while not on an empowering rune.
	/// </summary>
	private void RemoveActionsMatchingKnownSpells(EntityUid uid, BloodCultistComponent cultist)
	{
		// Get all actions for this cultist
		var actions = _action.GetActions(uid);
		
		// Create a set of spell IDs for quick lookup
		var knownSpellIds = new HashSet<ProtoId<CultAbilityPrototype>>(cultist.KnownSpells);

		// Remove actions that match spells in KnownSpells
		foreach (var actionId in actions)
		{
			if (!TryComp<CultistSpellComponent>(actionId, out var spellComp))
				continue;

			// Check if this action's AbilityId matches any spell in KnownSpells
			if (knownSpellIds.Contains(spellComp.AbilityId))
			{
				_action.RemoveAction((uid, null), (actionId, null));
			}
		}
	}

	private void OnStudyVeil(Entity<BloodCultistComponent> ent, ref EventCultistStudyVeil args)
	{
		if (!TryUseAbility(ent, args))
			return;

		ent.Comp.StudyingVeil = true;
		args.Handled = true;
	}

	private void OnCommune(Entity<BloodCultistComponent> ent, ref BloodCultCommuneSendMessage args)
	{
		ent.Comp.CommuningMessage = args.Message;
	}
/* Juggernauts using the commune ability, to be uncommented when juggernauts are added
	private void OnJuggernautCommune(Entity<JuggernautComponent> ent, ref BloodCultCommuneSendMessage args)
	{
		ent.Comp.CommuningMessage = args.Message;
	}
*/
	private void OnSpellSelectedMessage(Entity<BloodCultistComponent> ent, ref SpellsMessage args)
	{
		if (!CultistSpellComponent.ValidSpells.Contains(args.ProtoId) || ent.Comp.KnownSpells.Contains(args.ProtoId))
		{
			_popup.PopupEntity(Loc.GetString("cult-spell-havealready"), ent, ent);
			return;
		}
		AddSpell(ent, ent.Comp, args.ProtoId, recordKnownSpell:true);
	}

	private void OnSummonDagger(Entity<BloodCultistComponent> ent, ref EventCultistSummonDagger args)
	{
		if (!TryUseAbility(ent, args))
			return;

		var dagger = Spawn(_random.Pick(AvailableDaggers), Transform(ent).Coordinates);
		if (!_hands.TryForcePickupAnyHand(ent, dagger))
		{
			_popup.PopupEntity(Loc.GetString("cult-spell-fail"), ent, ent);
			QueueDel(dagger);
			return;
		}

		args.Handled = true;
	}

	/// <summary>
	/// Helper method to check if an entity is a cultist, including SSD cultists by checking their mind.
	/// </summary>
	private bool IsTargetCultist(EntityUid target)
	{
		// Check if the target's body has BloodCultistComponent
		if (HasComp<BloodCultistComponent>(target))
			return true;

		// Check if the target's mind has BloodCultistComponent (for SSD cultists)
		if (TryComp<MindContainerComponent>(target, out var mindContainer) && 
		    mindContainer.Mind != null &&
		    HasComp<BloodCultistComponent>(mindContainer.Mind.Value))
			return true;

		return false;
	}

	private void OnSanguineDream(Entity<BloodCultistComponent> ent, ref EventCultistSanguineDream args)
	{
		if (args.Handled)
			return;

		var target = args.Target;

		// Check if target is an allied cultist (including SSD cultists)
		if (IsTargetCultist(target))
		{
			_popup.PopupEntity(
				Loc.GetString("cult-spell-allied-cultist"),
				ent, ent, PopupType.MediumCaution
			);
			return;
		}

		if (!TryUseAbility(ent, args))
			return;

		args.Handled = true;

		// Mindshield protects from nocturine injection, but still stuns briefly
		if (HasComp<MindShieldComponent>(target))
		{
			// Stun the target briefly - this will make them drop prone and drop items
			if (TryComp<CrawlerComponent>(target, out var crawlerForStun))
			{
				_stun.TryKnockdown((target, crawlerForStun), TimeSpan.FromSeconds(3), true);
			}
			return;
		}

		int selfStunTime = 4;

		// Holy protection repels cult magic
		if (HasComp<CultResistantComponent>(target))
		{
			_popup.PopupEntity(
					Loc.GetString("cult-spell-repelled"),
					ent, ent, PopupType.MediumCaution
				);
			_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Effects/holy.ogg"), Transform(ent).Coordinates);
			// Knock down the cultist who cast the spell. Might need balancing
			if (TryComp<CrawlerComponent>(ent, out var crawler))
			{
				_stun.TryKnockdown((ent, crawler), TimeSpan.FromSeconds(selfStunTime), true);
			}
		}
		/* Juggernaut logic, so cultists don't stun them, to be uncommented when juggernauts are added
		else if (HasComp<JuggernautComponent>(target))
		{
			// Juggernauts are immune to sanguine dream (they have no bloodstream)
			_popup.PopupEntity(
				Loc.GetString("cult-spell-fail"),
				ent, ent, PopupType.MediumCaution
			);
		}
		*/
		else if (TryComp<BloodstreamComponent>(target, out var bloodstream))
		{
			// Stun the target - this will make them drop prone and drop items
			if (TryComp<CrawlerComponent>(target, out var crawlerForStun))
			{
				//Making this extra long to account for the nocturine slow onset time.
				_stun.TryKnockdown((target, crawlerForStun), TimeSpan.FromSeconds(5), true);
			}
			
			// Inject sleep chemicals (Nocturine + Edge Essentia) and a small amount of MuteToxin.
			// MuteToxin mutes for ~3 seconds (just until sleep kicks in); metabolism scale gives duration from amount.
			var sleepSolution = new Solution();
			sleepSolution.AddReagent((ProtoId<ReagentPrototype>)"Nocturine", FixedPoint2.New(15));  // 15u Nocturine
			sleepSolution.AddReagent((ProtoId<ReagentPrototype>)"EdgeEssentia", FixedPoint2.New(5));  // 5u Edge Essentia
			sleepSolution.AddReagent((ProtoId<ReagentPrototype>)"MuteToxin", FixedPoint2.New(0.15f));  // ~3s mute before sleep
			
			if (_solutionContainer.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out _))
			{
				if (bloodstream.BloodSolution != null)
					_solutionContainer.TryAddSolution(bloodstream.BloodSolution.Value, sleepSolution);
			}
			
			// Show the dream message
			_popup.PopupEntity(
				Loc.GetString("cult-spell-sleep-dream"),
				target, target, PopupType.LargeCaution
			);
			
			// Mark them for follow-up attacks
			// disabled for now, follow up attacks work, but end up being too fancy and not really needed.
			//EnsureComp<CultMarkedComponent>(target);
		}
	}


	private void OnTwistedConstruction(Entity<BloodCultistComponent> ent, ref EventCultistTwistedConstruction args)
	{
		// Check if target is a steel stack
		if (TryComp<StackComponent>(args.Target, out var stack) && stack.StackTypeId == "Steel")
		{
			if (!TryUseAbility(ent, args))
				return;

			var count = stack.Count;
			var targetCoords = Transform(args.Target).Coordinates;

			// Spawn runed steel stacks manually (SpawnMultiple doesn't exist)
			var runedSteelProto = _proto.Index(RunedSteelStack);
			var maxStackSize = _stack.GetMaxCount(RunedSteelStack);
			var stacksToSpawn = count;
			while (stacksToSpawn > 0)
			{
				var stackSize = Math.Min(stacksToSpawn, maxStackSize);
				var spawnedStack = Spawn(runedSteelProto.Spawn, targetCoords);
				if (TryComp<StackComponent>(spawnedStack, out var stackComponent))
				{
					_stack.SetCount((spawnedStack, stackComponent), stackSize);
				}
				stacksToSpawn -= stackSize;
			}

			QueueDel(args.Target);
			args.Handled = true;
			return;
		}

		// Check if target is a glass stack
		if (TryComp<StackComponent>(args.Target, out var glassStack) && glassStack.StackTypeId == "Glass")
		{
			if (!TryUseAbility(ent, args))
				return;

			var count = glassStack.Count;
			var targetCoords = Transform(args.Target).Coordinates;

			// Spawn runed glass stacks manually (SpawnMultiple doesn't exist)
			var runedGlassProto = _proto.Index(RunedGlassStack);
			var maxStackSize2 = _stack.GetMaxCount(RunedGlassStack);
			var stacksToSpawn2 = count;
			while (stacksToSpawn2 > 0)
			{
				var stackSize = Math.Min(stacksToSpawn2, maxStackSize2);
				var spawnedStack2 = Spawn(runedGlassProto.Spawn, targetCoords);
				if (TryComp<StackComponent>(spawnedStack2, out var stackComponent2))
				{
					_stack.SetCount((spawnedStack2, stackComponent2), stackSize);
				}
				stacksToSpawn2 -= stackSize;
			}

			QueueDel(args.Target);
			args.Handled = true;
			return;
		}

		// Check if target is a reinforced glass stack
		if (TryComp<StackComponent>(args.Target, out var reinforcedGlassStack) && reinforcedGlassStack.StackTypeId == "ReinforcedGlass")
		{
			if (!TryUseAbility(ent, args))
				return;

			var count = reinforcedGlassStack.Count;
			var targetCoords = Transform(args.Target).Coordinates;

			// Spawn runed glass stacks manually (SpawnMultiple doesn't exist)
			var runedGlassProto = _proto.Index(RunedGlassStack);
			var maxStackSize3 = _stack.GetMaxCount(RunedGlassStack);
			var stacksToSpawn3 = count;
			while (stacksToSpawn3 > 0)
			{
				var stackSize = Math.Min(stacksToSpawn3, maxStackSize3);
				var spawnedStack3 = Spawn(runedGlassProto.Spawn, targetCoords);
				if (TryComp<StackComponent>(spawnedStack3, out var stackComponent3))
				{
					_stack.SetCount((spawnedStack3, stackComponent3), stackSize);
				}
				stacksToSpawn3 -= stackSize;
			}

			QueueDel(args.Target);
			args.Handled = true;
			return;
		}

		// Check if target is a plasteel stack
		if (TryComp<StackComponent>(args.Target, out var plasteelStack) && plasteelStack.StackTypeId == "Plasteel")
		{
			if (!TryUseAbility(ent, args))
				return;

			var count = plasteelStack.Count;
			var targetCoords = Transform(args.Target).Coordinates;

			// Spawn runed plasteel stacks manually (SpawnMultiple doesn't exist)
			var runedPlasteelProto = _proto.Index(RunedPlasteelStack);
			var maxStackSize4 = _stack.GetMaxCount(RunedPlasteelStack);
			var stacksToSpawn4 = count;
			while (stacksToSpawn4 > 0)
			{
				var stackSize = Math.Min(stacksToSpawn4, maxStackSize4);
				var spawnedStack4 = Spawn(runedPlasteelProto.Spawn, targetCoords);
				if (TryComp<StackComponent>(spawnedStack4, out var stackComponent4))
				{
					_stack.SetCount((spawnedStack4, stackComponent4), stackSize);
				}
				stacksToSpawn4 -= stackSize;
			}

			QueueDel(args.Target);
			args.Handled = true;
			return;
		}
	}
}

