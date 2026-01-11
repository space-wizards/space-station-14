// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2026 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System;
using System.Linq;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Antag;
using Content.Shared.Antag;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Server.Speech.Prototypes;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Roles.Components;
using Content.Server.BloodCult.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Administration.Systems;
using Content.Shared.Administration.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.Clothing.Systems;
using Content.Shared.Body.Systems;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Roles.Jobs;
using Content.Shared.Localizations;
using Content.Shared.Pinpointer;
using Content.Shared.Ghost;
using Content.Shared.Database;
using Content.Server.Administration.Logs;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Bed.Sleep;

using Content.Server.BloodCult.EntitySystems;
using Content.Shared.BloodCult.Prototypes;

using Content.Server.Ghost;
using Content.Server.Ghost.Roles;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Raffles;
using Content.Server.Revolutionary.Components;
using Content.Server.Chat.Systems;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Shared.Console;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Emoting;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Blood Cults happen
/// </summary>
public sealed class BloodCultRuleSystem : GameRuleSystem<BloodCultRuleComponent>
{
	private const string JuggernautAccentPrototypeId = "juggernaut";
	
	private const string ActionCommune = "ActionCultistCommune";
	private const string ActionStudyVeil = "ActionCultistStudyVeil";
	private const string ActionSpellsSelect = "ActionCultistSpellsSelect";
	private const string ActionSummonDagger = "ActionCultistSummonDagger";
	
	private enum BloodStage
	{
		Rise,
		Veil
	}
	//Making this dynamic. Allows for better balancing because it keeps track of exactly how much blood the cult could ever get.
	private void PrepareNextStageRequirement(BloodCultRuleComponent component, BloodStage stage)
	{
		double totalRemaining = 0.0;

		foreach (var session in _playerManager.Sessions)
		{
			if (session.Status != SessionStatus.InGame)
				continue;

			if (session.AttachedEntity is not { } entity || Deleted(entity))
				continue;

			if (!TryComp<BloodstreamComponent>(entity, out _))
				continue;
			// Sums up the total remaining possible blood for each player
			var tracker = EnsureComp<BloodCollectionTrackerComponent>(entity);
			var remaining = Math.Max(0f, tracker.MaxBloodPerEntity - tracker.TotalBloodCollected);
			totalRemaining += remaining;
		}

		component.BloodCollected = 0.0;
		

		switch (stage)
		{
			case BloodStage.Rise:
			{
				// Change this to make the phase require different amounts of blood
				var required = totalRemaining / 10.0;
				component.BloodRequiredForRise = required;
				break;
			}
			case BloodStage.Veil:
			{
				// Change this to make the phase require different amounts of blood
				var required = totalRemaining / 10.0;
				component.BloodRequiredForVeil = required;
				break;
			}
		}
	}

	private void CompleteRiseStage(BloodCultRuleComponent component, List<EntityUid> cultists)
	{
		if (component.HasRisen)
			return;

		component.HasRisen = true;
		PrepareNextStageRequirement(component, BloodStage.Veil);
		AnnounceStatus(component, cultists);

		foreach (var cultist in cultists)
		{
			if (!TryComp<BloodCultistComponent>(cultist, out var cultistComp))
				continue;

			cultistComp.ShowTearVeilRune = true;
			DirtyField(cultist, cultistComp, nameof(BloodCultistComponent.ShowTearVeilRune));
		}
	}

	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly AntagSelectionSystem _antag = default!;
	[Dependency] private readonly MindSystem _mind = default!;
	[Dependency] private readonly RoleSystem _role = default!;
	[Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
	[Dependency] private readonly PopupSystem _popupSystem = default!;
	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly IGameTiming _timing = default!;
	[Dependency] private readonly GameTicker _gameTicker = default!;
	[Dependency] private readonly IPlayerManager _playerManager = default!;
	[Dependency] private readonly ChatSystem _chat = default!;
	[Dependency] private readonly SharedPhysicsSystem _physics = default!;
	[Dependency] private readonly SharedJobSystem _jobs = default!;
	[Dependency] private readonly RoundEndSystem _roundEnd = default!;
	[Dependency] private readonly MobStateSystem _mobSystem = default!;
	[Dependency] private readonly IChatManager _chatManager = default!;
	[Dependency] private readonly SharedBodySystem _body = default!;
	[Dependency] private readonly AppearanceSystem _appearance = default!;
	[Dependency] private readonly NpcFactionSystem _npcFaction = default!;
	[Dependency] private readonly IAdminLogManager _adminLogger = default!;
	[Dependency] private readonly IConsoleHost _consoleHost = default!;
	//[Dependency] private readonly SharedTransformSystem _transformSystem = default!;
	[Dependency] private readonly BloodCultMindShieldSystem _mindShield = default!;
	[Dependency] private readonly SleepingSystem _sleeping = default!;
	[Dependency] private readonly IPrototypeManager _proto = default!;
	[Dependency] private readonly SharedActionsSystem _action = default!;
	[Dependency] private readonly ActionContainerSystem _actionContainer = default!;
	[Dependency] private readonly SharedPointLightSystem _pointLight = default!;
	[Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

	public readonly string CultComponentId = "BloodCultist";

	private static readonly EntProtoId MindRole = "MindRoleCultist";

	public static readonly ProtoId<NpcFactionPrototype> BloodCultistFactionId = "BloodCultist";
    public static readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

	public override void Initialize()
	{
		base.Initialize();
		//SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);

		// Do we need a special "head" cultist? Don't think so
        //SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);

		SubscribeLocalEvent<BloodCultRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected); // Funky Station
        SubscribeLocalEvent<BloodCultRoleComponent, GetBriefingEvent>(OnGetBriefing);

		SubscribeLocalEvent<BloodCultistComponent, ReviveRuneAttemptEvent>(TryReviveCultist);
		SubscribeLocalEvent<BloodCultistComponent, GhostifyRuneEvent>(TryGhostifyCultist);
		SubscribeLocalEvent<BloodCultistComponent, SacrificeRuneEvent>(TrySacrificeVictim);
		SubscribeLocalEvent<BloodCultistComponent, ConvertRuneEvent>(TryConvertVictim);

		SubscribeLocalEvent<BloodCultistComponent, MindAddedMessage>(OnMindAdded);
		SubscribeLocalEvent<BloodCultistComponent, MindRemovedMessage>(OnMindRemoved);
		SubscribeLocalEvent<BloodCultistComponent, ComponentRemove>(OnCultistRemoved);
		
		// Ensure halos are applied when AppearanceComponent is added to cultists
		SubscribeLocalEvent<AppearanceComponent, ComponentStartup>(OnAppearanceStartup);

		// Do we need a special "head" cultist? Don't think so
		//SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);

		// Register admin commands
		InitializeCommands();
	}

	private void InitializeCommands()
	{
		_consoleHost.RegisterCommand("cult_queryblood",
			"Query the current blood collected and remaining for the Blood Cult game rule",
			"cult_queryblood",
			QueryBloodCommand);

		_consoleHost.RegisterCommand("cult_setblood",
			"Set the current blood amount for the Blood Cult game rule",
			"cult_setblood <amount>",
			SetBloodCommand);
	}

	protected override void Started(EntityUid uid, BloodCultRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
		// Calculate blood requirements based on player count instead of selecting targets
		CalculateBloodRequirements(component);
		component.InitialReportTime = _timing.CurTime + TimeSpan.FromSeconds(1);
		SetConversionsNeeded(component);
		SetMinimumCultistsForVeilRitual(component);
		SelectVeilTargets(component);
    }

	/// <summary>
	/// Calculates blood requirements for each phase based on current player count.
	/// Starts based on readyup count. Later on it calculates based on how many players could ever count.
	/// todo: Make it not count people in the ghost bar. No idea how to do that.
	/// </summary>
	private void CalculateBloodRequirements(BloodCultRuleComponent component)
	{
		var readyCount = Math.Max(0, _gameTicker.ReadyPlayerCount());
		var groups = Math.Max(1.0, Math.Ceiling(readyCount / 30.0));
		component.BloodRequiredForEyes = groups * 100.0;
		component.BloodRequiredForRise = 0.0; //Calculated later in the round
		component.BloodRequiredForVeil = 0.0; //Calculated later in the round
		component.BloodCollected = 0.0;
	}

	private void SelectVeilTargets(BloodCultRuleComponent component)
	{
		var beaconsList = new List<WeakVeilLocation>();

        var beacons = AllEntityQuery<NavMapBeaconComponent, MetaDataComponent>();
        while (beacons.MoveNext(out var beaconUid, out var navMapBeacon, out var metaData))
        {
			if (metaData.EntityPrototype != null &&
				metaData.EntityPrototype.EditorSuffix != null &&
				BloodCultRuleComponent.PossibleVeilLocations.Contains(metaData.EntityPrototype.ID))
			{
				var veilLoc = new WeakVeilLocation(
					metaData.EntityPrototype.EditorSuffix, beaconUid,
					metaData.EntityPrototype.ID, Transform(beaconUid).Coordinates,
					5.0f
				);
				beaconsList.Add(veilLoc);
			}
        }
		// Todo add something complicated here if there are less than 3 station beacons.
		// Which should never happen, but would totally break the game mode. So I'm having it fallback and just return with no beacon set.
		if (beaconsList.Count < 3)
			return;
		int first = _random.Next(0, beaconsList.Count);
		int second = _random.Next(0, beaconsList.Count);
		while (second == first)
			second = _random.Next(0, beaconsList.Count);
		int third = _random.Next(0, beaconsList.Count);
		while (third == second || third == first)
			third = _random.Next(0, beaconsList.Count);

		component.WeakVeil1 = beaconsList[first];
		component.WeakVeil2 = beaconsList[second];
		component.WeakVeil3 = beaconsList[third];
	}

	private void AfterEntitySelected(Entity<BloodCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, ent);
    }

	/// <summary>
	/// Checks if an action container already contains an action with the specified prototype ID.
	/// </summary>
	private bool HasActionWithPrototype(ActionsContainerComponent container, string prototypeId)
	{
		foreach (var actionId in container.Container.ContainedEntities)
		{
			if (MetaData(actionId).EntityPrototype?.ID == prototypeId)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Checks if an actions component already contains an action with the specified prototype ID.
	/// </summary>
	private bool HasActionWithPrototype(ActionsComponent actions, string prototypeId)
	{
		foreach (var actionId in actions.Actions)
		{
			if (MetaData(actionId).EntityPrototype?.ID == prototypeId)
				return true;
		}
		return false;
	}

	/// <summary>
    /// Supplies new cultists with what they need.
    /// </summary>
    /// <returns>true if cultist was successfully added.</returns>

	private bool MakeCultist(EntityUid traitor, BloodCultRuleComponent component)
    {
        if (_TryAssignCultMind(traitor))
		{
		// add cultist starting abilities - directly add actions to ensure they show up on action bar
		// Follow the pattern used by StoreSystem: add to mind container if available, otherwise to entity
		// Check if actions already exist to avoid duplicates
		if (_mind.TryGetMind(traitor, out var mindId, out _))
		{
			// Check if actions already exist in the mind's action container
			if (TryComp<ActionsContainerComponent>(mindId, out var mindContainer))
			{
				if (!HasActionWithPrototype(mindContainer, ActionCommune))
					_actionContainer.AddAction(mindId, ActionCommune);
				if (!HasActionWithPrototype(mindContainer, ActionStudyVeil))
					_actionContainer.AddAction(mindId, ActionStudyVeil);
				if (!HasActionWithPrototype(mindContainer, ActionSpellsSelect))
					_actionContainer.AddAction(mindId, ActionSpellsSelect);
				if (!HasActionWithPrototype(mindContainer, ActionSummonDagger))
					_actionContainer.AddAction(mindId, ActionSummonDagger);
			}
			else
			{
				// No container yet, safe to add
				_actionContainer.AddAction(mindId, ActionCommune);
				_actionContainer.AddAction(mindId, ActionStudyVeil);
				_actionContainer.AddAction(mindId, ActionSpellsSelect);
				_actionContainer.AddAction(mindId, ActionSummonDagger);
			}
		}
		else
		{
			// Fallback: add directly to entity if mind isn't available
			// Check if actions already exist on the entity
			if (TryComp<ActionsComponent>(traitor, out var entityActions))
			{
				if (!HasActionWithPrototype(entityActions, ActionCommune))
					_action.AddAction(traitor, ActionCommune);
				if (!HasActionWithPrototype(entityActions, ActionStudyVeil))
					_action.AddAction(traitor, ActionStudyVeil);
				if (!HasActionWithPrototype(entityActions, ActionSpellsSelect))
					_action.AddAction(traitor, ActionSpellsSelect);
				if (!HasActionWithPrototype(entityActions, ActionSummonDagger))
					_action.AddAction(traitor, ActionSummonDagger);
			}
			else
			{
				// No actions component yet, safe to add
				_action.AddAction(traitor, ActionCommune);
				_action.AddAction(traitor, ActionStudyVeil);
				_action.AddAction(traitor, ActionSpellsSelect);
				_action.AddAction(traitor, ActionSummonDagger);
			}
		}

			// Ensure blood cultist can see antag icons (required for status icon visibility)
			EnsureComp<ShowAntagIconsComponent>(traitor);

			// Register UI components for Commune and Prepare Spell actions
			var userInterfaceComp = EnsureComp<UserInterfaceComponent>(traitor);
			_uiSystem.SetUi((traitor, userInterfaceComp), BloodCultistCommuneUIKey.Key, new InterfaceData("BloodCultCommuneBoundUserInterface"));
			_uiSystem.SetUi((traitor, userInterfaceComp), SpellsUiKey.Key, new InterfaceData("SpellsBoundUserInterface"));

			if (TryComp<BloodCultistComponent>(traitor, out var cultist))
			{
				// propogate the selected Nar'Sie summon location
				// Enable Tear Veil rune if stage 2 (HasRisen) or later has been reached
				cultist.ShowTearVeilRune = component.HasRisen || component.VeilWeakened;
				cultist.LocationForSummon = component.LocationForSummon;
			}

			if (component.HasEyes)
			{
				// Ensure AppearanceComponent exists before setting eyes visual
				// Only enable eyes if the body has an attached head
				var appearance = EnsureComp<AppearanceComponent>(traitor);
				var hasHead = false;
				if (TryComp<BodyComponent>(traitor, out var body))
				{
					var head = _body.GetBodyChildrenOfType(traitor, BodyPartType.Head, body).FirstOrDefault();
					hasHead = head.Id != EntityUid.Invalid;
				}
				_appearance.SetData(traitor, CultEyesVisuals.CultEyes, hasHead, appearance);
			}

			if (component.VeilWeakened)
			{
				// Ensure AppearanceComponent exists before setting halo visual
				var appearance = EnsureComp<AppearanceComponent>(traitor);
				_appearance.SetData(traitor, CultHaloVisuals.CultHalo, true, appearance);
				UpdateCultHaloLight(traitor, true);
			}

			_npcFaction.RemoveFaction(traitor, NanotrasenFactionId, false);
			_npcFaction.AddFaction(traitor, BloodCultistFactionId);

			return true;
		}
		return false;
	}

	private bool _TryAssignCultMind(EntityUid traitor)
	{
		if (!_mind.TryGetMind(traitor, out var mindId, out var mind))
            return false;

		_role.MindAddRole(mindId, MindRole, mind, true);

		EnsureComp<BloodCultistComponent>(traitor);

        _antag.SendBriefing(traitor, Loc.GetString("cult-role-greeting"), Color.Red, null);

        if (TryComp<MindComponent>(mindId, out var mindComp) && _role.MindHasRole<BloodCultRoleComponent>((mindId, mindComp), out var cultRoleComp))
			AddComp(cultRoleComp.Value, new RoleBriefingComponent { Briefing = Loc.GetString("cult-briefing") }, overwrite: true);
            //AddComp(cultRoleComp.Value, new RoleBriefingComponent { Briefing = Loc.GetString("head-rev-briefing", ("code", string.Join("-", code).Replace("sharp", "#"))) }, overwrite: true);

        return true;
	}


	protected override void ActiveTick(EntityUid uid, BloodCultRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
		List<EntityUid> cultists = GetCultists();

		// Give initial announcement to cultists
		if (component.InitialReportTime != null && _timing.CurTime > component.InitialReportTime)
		{
			AnnounceStatus(component, cultists);
			component.InitialReportTime = null;
		}

		if (component.VeilWeakened && !component.VeilWeakenedAnnouncementPlayed)
		{
			AnnounceStatus(component, cultists);
			component.VeilWeakenedAnnouncementPlayed = true;
			foreach (EntityUid cultist in cultists)
			{
				if (!TryComp<BloodCultistComponent>(cultist, out var cultistComp))
					continue;
				cultistComp.ShowTearVeilRune = true;
				DirtyField(cultist, cultistComp, nameof(BloodCultistComponent.ShowTearVeilRune));
			}
		}

		// Handle deferred spawning of the blood anomaly and runes once the veil is weakened
		if (component.BloodAnomalySpawnScheduled && component.BloodAnomalySpawnTime != null)
		{
			if (_timing.CurTime >= component.BloodAnomalySpawnTime)
			{
				var riftSetup = EntityManager.System<BloodCult.EntitySystems.BloodCultRiftSetupSystem>();
				var rift = riftSetup.TrySetupRitualSite(component);

				if (rift != null)
				{
					component.BloodAnomalySpawnScheduled = false;
					component.BloodAnomalySpawned = true;
					component.BloodAnomalySpawnTime = null;
					component.BloodAnomalyUid = rift;

					if (component.LocationForSummon != null)
					{
						var summonLocation = (WeakVeilLocation) component.LocationForSummon;
						foreach (var cultist in cultists)
						{
							if (!TryComp<BloodCultistComponent>(cultist, out var cultistComp))
								continue;
							cultistComp.LocationForSummon = summonLocation;
							DirtyField(cultist, cultistComp, nameof(BloodCultistComponent.LocationForSummon));
						}
					}

					foreach (var cultist in cultists)
					{
						_popupSystem.PopupEntity(
							Loc.GetString("cult-rift-spawned"),
							cultist, cultist, PopupType.LargeCaution);
					}

					if (component.LocationForSummon != null)
					{
						var summonLocation = (WeakVeilLocation) component.LocationForSummon;
						var message = Loc.GetString("cult-central-rift-warning", ("location", summonLocation.Name));
						_chat.DispatchGlobalAnnouncement(message, "Central Command");
					}

					AnnounceStatus(component, cultists);
				}
				else
				{
					component.BloodAnomalySpawnTime = _timing.CurTime + TimeSpan.FromSeconds(30);
				}
			}
		}

		// Check if blood thresholds have been reached for stage progression
		if (!component.HasEyes && component.BloodCollected >= component.BloodRequiredForEyes)
		{
			component.HasEyes = true;
			EmpowerCultists(cultists);
			AnnounceStatus(component, cultists);
			PrepareNextStageRequirement(component, BloodStage.Rise);

			if (component.BloodRequiredForRise <= 0)
				CompleteRiseStage(component, cultists);
		}

		if (component.HasEyes && !component.HasRisen && component.BloodRequiredForRise > 0 && component.BloodCollected >= component.BloodRequiredForRise)
		{
			CompleteRiseStage(component, cultists);
		}

		// Stage 3 (VeilWeakened) requires the Tear the Veil ritual to be completed
		// This is handled by the TearVeilSystem and cannot be triggered by blood collection alone

		// Disabled: Conversion-based progression conflicts with blood-based progression
		// The blood system provides better gameplay and doesn't trigger prematurely with low player counts
		// if (!component.HasEyes && GetConversionsToEyes(component, cultists) == 0)
		// {
		// 	component.HasEyes = true;
		// 	EmpowerCultists(cultists);
		// }
		//
		// if (!component.HasRisen && GetConversionsToRise(component, cultists) == 0)
		// {
		// 	component.HasRisen = true;
		// 	RiseCultists(cultists);
		// }

		foreach (EntityUid cultistUid in cultists)
		{
			if (!TryComp<BloodCultistComponent>(cultistUid, out var cultist))
				continue;

			// Ensure ShowTearVeilRune is always correct based on current stage
			bool shouldShowTearVeil = component.HasRisen || component.VeilWeakened;
			if (cultist.ShowTearVeilRune != shouldShowTearVeil)
			{
				cultist.ShowTearVeilRune = shouldShowTearVeil;
				DirtyField(cultistUid, cultist, nameof(BloodCultistComponent.ShowTearVeilRune));
			}

			// Ensure halos are always present when veil is weakened
			// This handles edge cases where AppearanceComponent was added later or halo wasn't set
			if (component.VeilWeakened)
			{
				var appearance = EnsureComp<AppearanceComponent>(cultistUid);
				// Check if halo is already set to avoid unnecessary updates
				// Use TryGetData to check if the halo is already set to true
				if (!_appearance.TryGetData<bool>(cultistUid, CultHaloVisuals.CultHalo, out var haloValue, appearance) || !haloValue)
				{
					_appearance.SetData(cultistUid, CultHaloVisuals.CultHalo, true, appearance);
					UpdateCultHaloLight(cultistUid, true);
				}
				else
				{
					// Ensure light is present even if halo was already set
					UpdateCultHaloLight(cultistUid, true);
				}
			}

			// Show cult status
			if (cultist.StudyingVeil)
			{
				AnnounceStatus(component, cultists, cultistUid);
				cultist.StudyingVeil = false;
			}

			// Distribute cult communes
			if (cultist.CommuningMessage != null)
			{
				DistributeCommune(component, cultist.CommuningMessage, cultistUid);
				cultist.CommuningMessage = null;
			}

			// Apply active revives
			if (cultist.BeingRevived)
			{
				_ReviveCultist(cultistUid, cultist.ReviverUid);
				cultist.BeingRevived = false;
				cultist.ReviverUid = null;
			}

			// Apply active sacrifices
			if (cultist.Sacrifice != null)
			{
				SacrificingData sacrifice = (SacrificingData)cultist.Sacrifice;
				
				if (_SacrificeOffering(sacrifice, component, cultistUid))
				{
					AnnounceToCultist(Loc.GetString("cult-narsie-sacrifice-accept"), cultistUid, newlineNeeded:true);
					component.TotalSacrifices = component.TotalSacrifices + 1;
				}

				cultist.Sacrifice = null;
			}

			// Apply active converts
			if (cultist.Convert != null)
			{
				ConvertingData convert = (ConvertingData)cultist.Convert;
				_ConvertOffering(convert, component, cultistUid);
				cultist.Convert = null;
			}

			// Check for decultification
			if (cultist.DeCultification >= 100.0f)
			{
				_popupSystem.PopupEntity(Loc.GetString("cult-deconverted"),
					cultistUid, cultistUid, PopupType.LargeCaution
				);

				_mindShield.TryDeconvert(cultistUid, popupLocId: null, stunDuration: TimeSpan.Zero, log: false);
				continue;
			}

			// Did someone just fail to summon Nar'Sie?
			if (cultist.FailedNarsieSummon)
			{
				_popupSystem.PopupEntity(
					Loc.GetString("cult-invocation-narsie-fail"),
					cultistUid, cultistUid, PopupType.MediumCaution
				);
				cultist.FailedNarsieSummon = false;
			}

			// Summon Nar'Sie
			if (!component.CultistsWin && cultist.NarsieSummoned != null)
			{
				component.CultistsWin = true;
				string newlines = "\n\n\n\n";
				AnnounceToEveryone(newlines+Loc.GetString("cult-veil-torn")+newlines, fontSize:32, audioPath:"/Audio/Ambience/Antag/dimensional_rend.ogg", audioVolume:2f);
				var narsieSpawn = Spawn("MobNarsieSpawn", (EntityCoordinates)cultist.NarsieSummoned);
				cultist.NarsieSummoned = null;
				component.CultVictoryEndTime = _timing.CurTime + component.CultVictoryEndDelay;
			}
		}

		// Process juggernaut communes
		var juggernauts = AllEntityQuery<JuggernautComponent>();
		while (juggernauts.MoveNext(out var juggernautUid, out var juggernaut))
		{
			if (juggernaut.CommuningMessage != null)
			{
				DistributeCommune(component, juggernaut.CommuningMessage, juggernautUid);
				juggernaut.CommuningMessage = null;
			}
		}

		// End the round
		if (component.CultistsWin && !component.CultVictoryAnnouncementPlayed && component.CultVictoryEndTime != null && _timing.CurTime >= component.CultVictoryEndTime)
		{
			component.CultVictoryAnnouncementPlayed = true;
			component.CultVictoryEndTime = null;
			
			// Play the cult win announcement before ending the round
			_chat.DispatchGlobalAnnouncement(
				Loc.GetString("cult-win-announcement"),
				"Central Command",
				colorOverride: Color.Gold
			);
			
			_roundEnd.EndRound();
			return;
		}
    }

	private void EndRound()
    {
        _roundEnd.EndRound();
    }

	// todo: This doesn't count correctly. And it happens after Nar'Sie is summoned and has already eaten at least all of the cultists on the final ritual site.
	protected override void AppendRoundEndText(EntityUid uid, BloodCultRuleComponent component, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var sessionData = _antag.GetAntagIdentifiers(uid);
		if (component.CultistsWin)
			args.AddLine(Loc.GetString("cult-roundend-victory"));
		else
			args.AddLine(Loc.GetString("cult-roundend-failure"));
		args.AddLine(Loc.GetString("cult-roundend-count", ("count", component.TotalConversions.ToString())));
		args.AddLine(Loc.GetString("cult-roundend-sacrifices", ("sacrifices", component.TotalSacrifices.ToString())));
    }

	private List<EntityUid> GetEveryone(bool includeGhosts = false)
	{
		var everyoneList = new List<EntityUid>();

        var everyone = AllEntityQuery<ActorComponent, MobStateComponent>();
        while (everyone.MoveNext(out var uid, out var actorComp, out _))
        {
            everyoneList.Add(uid);
        }
		if (includeGhosts)
		{
			var ghosts = AllEntityQuery<GhostHearingComponent, ActorComponent>();
			while (ghosts.MoveNext(out var uid, out var _, out var actorComp))
			{
				everyoneList.Add(uid);
			}
		}

        return everyoneList;
	}

	// todo: Maybe make a more performant version of this. 
	// I don't think it calls this get cultists check too frequently though, I didn't make any on-tick events that should be calling this, so it's not spawning an on-tick all-entity query.
	private List<EntityUid> GetCultists(bool includeConstructs = false)
    {
        var cultistList = new List<EntityUid>();

        var cultists = AllEntityQuery<BloodCultistComponent, MobStateComponent>();
		var constructs = AllEntityQuery<BloodCultConstructComponent, MobStateComponent>();
        while (cultists.MoveNext(out var uid, out var cultistComp, out _))
        {
            cultistList.Add(uid);
        }
		if (includeConstructs)
		{
			while (constructs.MoveNext(out var uid, out var constructComp, out _))
			{
				cultistList.Add(uid);
			}
		}

        return cultistList;
    }

	public bool TryGetActiveRule(out BloodCultRuleComponent component)
	{
		var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
		while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
		{
			if (!GameTicker.IsGameRuleActive(uid, gameRule))
				continue;

			component = ruleComp;
			return true;
		}

		component = default!;
		return false;
	}

	private void OnGetBriefing(EntityUid uid, BloodCultRoleComponent comp, ref GetBriefingEvent args)
    {
		args.Append(Loc.GetString("cult-briefing-targets"));
    }

	public void TryReviveCultist(EntityUid uid, BloodCultistComponent comp, ref ReviveRuneAttemptEvent args)
	{
		comp.ReviverUid = args.User;
		comp.BeingRevived = true;
	}

	// todo: Make it not heal the cultist fully, or make them bleeding or something? Balancing issue.
	private void _ReviveCultist(EntityUid uid, EntityUid? casterUid)
	{
		Speak(casterUid, Loc.GetString("cult-invocation-revive"));
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/staff_healing.ogg"), uid);
		_rejuvenate.PerformRejuvenate(uid);
	}
	
	// Don't think this is used anymore.
	private void TryGhostifyCultist(EntityUid uid, BloodCultistComponent comp, ref GhostifyRuneEvent args)
	{
		if (HasComp<GhostRoleComponent>(uid) || HasComp<GhostTakeoverAvailableComponent>(uid))
			return;

		/*
		var settings = new GhostRoleRaffleSettings()
		{
			InitialDuration = initial,
			JoinExtendsDurationBy = extends,
			MaxDuration = max
		};
		var ghostRoleInfo = new GhostRoleInfo//()
		{
			Identifier = id,
			Name = role.RoleName,
			Description = role.RoleDescription,
			Rules = role.RoleRules,
			Requirements = role.Requirements,
			Kind = kind,
			RafflePlayerCount = rafflePlayerCount,
			RaffleEndTime = raffleEndTime
		};
		*/

		GhostRoleRaffleSettings settings;
		settings = new GhostRoleRaffleSettings()
		{
			InitialDuration = 20,
			JoinExtendsDurationBy = 5,
			MaxDuration = 30
		};

		GhostRoleComponent ghostRole = AddComp<GhostRoleComponent>(uid);
		EnsureComp<GhostTakeoverAvailableComponent>(uid);
		ghostRole.RoleName = Loc.GetString("cult-ghost-role-name");
		ghostRole.RoleDescription = Loc.GetString("cult-ghost-role-desc");
		ghostRole.RoleRules = Loc.GetString("cult-ghost-role-rules");
		ghostRole.RaffleConfig = new GhostRoleRaffleConfig(settings);
		Speak(args.User, Loc.GetString("cult-invocation-revive"));
	}

	public void TrySacrificeVictim(EntityUid uid, BloodCultistComponent comp, ref SacrificeRuneEvent args)
	{
		comp.Sacrifice = new  SacrificingData(args.Victim, args.Invokers);
	}

	private bool _SacrificeOffering(SacrificingData sacrifice, BloodCultRuleComponent component, EntityUid cultistUid)
	{
		if (HasComp<CultResistantComponent>(sacrifice.Victim))
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail-resisted"),
				cultistUid, cultistUid, PopupType.MediumCaution
			);
			return false;
		}
		else if (sacrifice.Invokers.Length < component.CultistsToSacrifice)
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail"),
				cultistUid, cultistUid, PopupType.MediumCaution
			);
			return false;
		}
	else
	{
		// Only make the minimum required number of cultists speak
		int speakerCount = 0;
		foreach (EntityUid invoker in sacrifice.Invokers)
		{
			if (speakerCount >= component.CultistsToSacrifice)
				break;
			
			Speak(invoker, Loc.GetString("cult-invocation-offering"));
			speakerCount++;
		}

		if (_SacrificeVictim(sacrifice.Victim, cultistUid))
		{
			return true;
		}
	}
	return false;
	}

	private bool _SacrificeVictim(EntityUid uid, EntityUid? casterUid)
	{
		// Remember to use coordinates to play audio if the entity is about to vanish.
		EntityUid? mindId = CompOrNull<MindContainerComponent>(uid)?.Mind;
		MindComponent? mindComp = CompOrNull<MindComponent>(mindId);
		if (mindId != null && mindComp != null)
		{
			var coordinates = Transform(uid).Coordinates;
			
			// Check if the victim is Hamlet to spawn hamstone instead of soulstone
			// Get speech component and accent component BEFORE gibbing the body
			var victimMeta = MetaData(uid);
			var isHamlet = victimMeta.EntityPrototype?.ID == "MobHamsterHamlet";
			SpeechComponent? victimSpeech = null;
			ReplacementAccentComponent? victimAccent = null;
			if (isHamlet)
			{
				TryComp<SpeechComponent>(uid, out victimSpeech);
				TryComp<ReplacementAccentComponent>(uid, out victimAccent);
			}
			
			_audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/disintegrate.ogg"), coordinates);
			var soulstonePrototype = isHamlet ? "CultHamstone" : "CultSoulStone";
			
			_body.GibBody(uid, true);
			var soulstone = Spawn(soulstonePrototype, coordinates);
			_mind.TransferTo((EntityUid)mindId, soulstone, mind:mindComp);
			
			// Preserve speech component and speech restrictions (ReplacementAccentComponent) from Hamlet if applicable
			if (isHamlet && victimSpeech != null)
			{
				CopyComp(uid, soulstone, victimSpeech);
				// Copy ReplacementAccentComponent if it exists (preserves speech restrictions like cognizine requirement)
				if (victimAccent != null)
				{
					CopyComp(uid, soulstone, victimAccent);
				}
			}
			else
			{
				// Ensure the soulstone can speak but not move
				EnsureComp<SpeechComponent>(soulstone);
			}
			EnsureComp<EmotingComponent>(soulstone);
		
		// Give the soulstone a physics push for visual effect
		if (TryComp<PhysicsComponent>(soulstone, out var physics))
		{
			// Wake the physics body so it responds to the impulse
			_physics.SetAwake((soulstone, physics), true);
			
			// Generate a random direction and speed (5-10 units/sec similar to a weak throw)
			var randomDirection = _random.NextVector2();
			var speed = _random.NextFloat(5f, 10f);
			var impulse = randomDirection * speed * physics.Mass;
			_physics.ApplyLinearImpulse(soulstone, impulse, body: physics);
		}
			
			return true;
		}
		return false;
	}

	public void TryConvertVictim(EntityUid uid, BloodCultistComponent comp, ref ConvertRuneEvent args)
	{
		comp.Convert = new ConvertingData(args.Subject, args.Invokers);
	}

	private bool _ConvertOffering(ConvertingData convert, BloodCultRuleComponent component, EntityUid cultistUid)
	{
		if (HasComp<CultResistantComponent>(convert.Subject))
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail-resisted"),
				cultistUid, cultistUid, PopupType.MediumCaution
			);
			return false;
		}
		else if (convert.Invokers.Length >= component.CultistsToConvert)
		{
			// Only make the minimum required number of cultists speak
			int speakerCount = 0;
			foreach (EntityUid invoker in convert.Invokers)
			{
				if (speakerCount >= component.CultistsToConvert)
					break;
				
				Speak(invoker, Loc.GetString("cult-invocation-offering"));
				speakerCount++;
			}
			
			_ConvertVictim(convert.Subject, component);
			return true;
		}
		else
		{
			_popupSystem.PopupEntity(
				Loc.GetString("cult-invocation-fail"),
				cultistUid, cultistUid, PopupType.MediumCaution
			);
			return false;
		}
	}

	private void _ConvertVictim(EntityUid uid, BloodCultRuleComponent component)
	{
		_audio.PlayPvs(new SoundPathSpecifier("/Audio/Ambience/Antag/creepyshriek.ogg"), uid);
		MakeCultist(uid, component);
		_rejuvenate.PerformRejuvenate(uid);
		
		// Increment conversion counter
		component.TotalConversions++;
		
		// Wake up sleeping players 
		if (TryComp<SleepingComponent>(uid, out var sleeping))
		{
			_sleeping.TryWaking((uid, sleeping), force: true);
		}
	}

	private void OnMindAdded(EntityUid uid, BloodCultistComponent cultist, MindAddedMessage args)
	{
		_TryAssignCultMind(uid);
	}

	private void OnMindRemoved(EntityUid uid, BloodCultistComponent cultist, MindRemovedMessage args)
	{
		_role.MindRemoveRole<BloodCultRoleComponent>(args.Mind.Owner);
		CheckCultistCountAndCallEvac();
	}

	private void OnCultistRemoved(EntityUid uid, BloodCultistComponent cultist, ComponentRemove args)
	{
		CheckCultistCountAndCallEvac();
	}

	/// <summary>
	/// When AppearanceComponent is added to an entity, ensure cult visuals (eyes/halo) are applied if applicable.
	/// This handles edge cases where AppearanceComponent is added after the cult has progressed.
	/// </summary>
	private void OnAppearanceStartup(EntityUid uid, AppearanceComponent appearance, ComponentStartup args)
	{
		// Only process for cultists
		if (!TryComp<BloodCultistComponent>(uid, out var cultist))
			return;

		// Check if we have an active rule to get stage information
		if (!TryGetActiveRule(out var ruleComp))
			return;

		// Apply eyes visual if stage 1 (HasEyes) or later has been reached
		// But only if the body has an attached head
		if (ruleComp.HasEyes)
		{
			var hasHead = false;
			if (TryComp<BodyComponent>(uid, out var body))
			{
				var head = _body.GetBodyChildrenOfType(uid, BodyPartType.Head, body).FirstOrDefault();
				hasHead = head.Id != EntityUid.Invalid;
			}
			_appearance.SetData(uid, CultEyesVisuals.CultEyes, hasHead, appearance);
		}

		// Apply halo visual if stage 3 (VeilWeakened) has been reached
		if (ruleComp.VeilWeakened)
		{
			_appearance.SetData(uid, CultHaloVisuals.CultHalo, true, appearance);
			UpdateCultHaloLight(uid, true);
		}
	}

	private void UpdateCultEyesBasedOnHead(EntityUid bodyUid)
	{
		// Only process for cultists
		if (!TryComp<BloodCultistComponent>(bodyUid, out _))
			return;

		// Check if we have an active rule to get stage information
		if (!TryGetActiveRule(out var ruleComp) || !ruleComp.HasEyes)
			return;

		// Check if body has an attached head
		// How they got this far without a head is questionable
		var hasHead = false;
		if (TryComp<BodyComponent>(bodyUid, out var body))
		{
			var head = _body.GetBodyChildrenOfType(bodyUid, BodyPartType.Head, body).FirstOrDefault();
			hasHead = head.Id != EntityUid.Invalid;
		}

		// Update eyes visual based on whether head exists
		if (TryComp<AppearanceComponent>(bodyUid, out var appearance))
		{
			_appearance.SetData(bodyUid, CultEyesVisuals.CultEyes, hasHead, appearance);
		}
	}



	private void CheckCultistCountAndCallEvac()
	{
		// Only check if there's an active rule
		if (!TryGetActiveRule(out var rule))
			return;

		// Don't call evac if it's already been called
		if (_roundEnd.IsRoundEndRequested())
			return;

		// Get all cultists (excluding constructs)
		var cultists = GetCultists(includeConstructs: false);
		var cultistCount = cultists.Count;

		// Call evac if cult drops to 0 or 1 members
		if (cultistCount <= 1)
		{
			_roundEnd.RequestRoundEnd(
				TimeSpan.FromMinutes(10),
				null,
				false,
				"cult-evac-called-announcement",
				"cult-evac-sender-announcement"
			);
		}
	}

	public void Speak(EntityUid? uid, string speech, bool forceLoud = false)
	{
		if (uid == null || string.IsNullOrWhiteSpace(speech))
			return;

		if (!Loc.TryGetString(speech, out var message))
			message = speech;

		OnBloodCultSpellSpoken(uid.Value, message, forceLoud);
	}

	private void OnBloodCultSpellSpoken(EntityUid performer, string speech, bool forceLoud)
	{
		var chatType = InGameICChatType.Speak;

		if (!forceLoud)
		{
			if (!TryGetActiveRule(out var component) || !component.VeilWeakened)
				chatType = InGameICChatType.Whisper;
		}

		_chat.TrySendInGameICMessage(performer, speech, chatType, false);
	}

	/// <summary>
	/// Generates a random cult chant by combining phrases from the cult-chants.ftl localization file.
	/// </summary>
	/// <param name="wordCount">Number of words in the chant (default: 2)</param>
	/// <returns>A randomly generated cult chant</returns>
	public string GenerateChant(int wordCount = 2)
	{
		const int totalChants = 15; // Total number of cult-chant-X entries in cult-chants.ftl
		
		if (wordCount < 1)
			wordCount = 1;
		
		var chantParts = new List<string>();
		for (int i = 0; i < wordCount; i++)
		{
			var chantIndex = Random.Shared.Next(1, totalChants + 1);
			chantParts.Add(Loc.GetString($"cult-chant-{chantIndex}"));
		}
		
		return string.Join(" ", chantParts);
	}

	public void AnnounceToEveryone(string message, uint fontSize = 14, Color? color = null,
									bool newlineNeeded = false, string? audioPath = null,
									float audioVolume = 1f)
	{
		if (color == null)
			color = Color.DarkRed;
		var filter = Filter.Empty();
		List<EntityUid> everyone = GetEveryone(includeGhosts:true);
		foreach (EntityUid playerUid in everyone)
		{
			if (TryComp(playerUid, out ActorComponent? actorComp))
			{
				filter.AddPlayer(actorComp.PlayerSession);
			}
		}
		string wrappedMessage = "[font size="+fontSize.ToString()+"][bold]" + (newlineNeeded ? "\n" : "") + message + "[/bold][/font]";
		_chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, default, false, true, color, audioPath, audioVolume);
	}

	public void AnnounceToCultists(string message, uint fontSize = 14, Color? color = null,
									bool newlineNeeded = false, string? audioPath = null,
									float audioVolume = 1f, bool includeGhosts = false)
	{
		if (color == null)
			color = Color.DarkRed;
		var filter = Filter.Empty();
		List<EntityUid> cultists = GetCultists(includeConstructs:true);
		foreach (EntityUid cultistUid in cultists)
		{
			if (TryComp(cultistUid, out ActorComponent? actorComp))
			{
				filter.AddPlayer(actorComp.PlayerSession);
			}
		}
		if (includeGhosts)
		{
			var ghosts = AllEntityQuery<GhostHearingComponent, ActorComponent>();
			while (ghosts.MoveNext(out var uid, out var _, out var actorComp))
			{
				filter.AddPlayer(actorComp.PlayerSession);
			}
		}

		string wrappedMessage = "[font size="+fontSize.ToString()+"][bold]" + (newlineNeeded ? "\n" : "") + message + "[/bold][/font]";
		_chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, default, false, true, color, audioPath, audioVolume);
		_adminLogger.Add(LogType.Chat, LogImpact.Low, $"Announcement to cultists: {message}");
	}

	public void AnnounceToCultist(string message, EntityUid target, uint fontSize = 14, Color? color = null,
									bool newlineNeeded = false, string? audioPath = null,
									float audioVolume = 1f)
	{
		if (color == null)
			color = Color.DarkRed;
		var filter = Filter.Empty();
		List<EntityUid> cultists = GetCultists(includeConstructs: true);
		foreach (EntityUid cultistUid in cultists)
		{
			if (TryComp(cultistUid, out ActorComponent? actorComp) && cultistUid == target)
			{
				filter.AddPlayer(actorComp.PlayerSession);
			}
		}
		string wrappedMessage = "[font size="+fontSize.ToString()+"][bold]" + (newlineNeeded ? "\n" : "") + message + "[/bold][/font]";
		_chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, default, false, true, color, audioPath, audioVolume);
	}

	private void SetConversionsNeeded(BloodCultRuleComponent component)
	{
		var allAliveHumans = _mind.GetAliveHumans();
		// 10% cult needed for eyes
		component.ConversionsUntilEyes = (int)Math.Ceiling((float)allAliveHumans.Count * 0.125f);
		// 30% cult needed for rise
		component.ConversionsUntilRise = (int)Math.Ceiling((float)allAliveHumans.Count * 0.3f);
	}

	/// <summary>
	/// Calculates the minimum number of cultists required for the Tear Veil ritual based on player count.
	/// Uses 1/8th of total players (12.5%), with a minimum of 2 cultists.
	/// </summary>
	private void SetMinimumCultistsForVeilRitual(BloodCultRuleComponent component)
	{
		var allAliveHumans = _mind.GetAliveHumans();
		// 5% of players, minimum of 2, maximum of 4
		// So at 20 players its 2, at 20-60 players its 3, at 60+ players its 4
		component.MinimumCultistsForVeilRitual = Math.Max(2, Math.Min(4,(int)Math.Ceiling((float)allAliveHumans.Count * 0.05f)));
	}

	private int GetConversionsToEyes(BloodCultRuleComponent component, List<EntityUid> cultists)
	{
		// Has the cultist group reached the needed conversions?
		if (component.HasEyes)
			return 0;
		int conversionsUntilEyes = component.ConversionsUntilEyes - cultists.Count;
		conversionsUntilEyes = (conversionsUntilEyes > 0) ? conversionsUntilEyes : 0;
		return conversionsUntilEyes;
	}

	private int GetConversionsToRise(BloodCultRuleComponent component, List<EntityUid> cultists)
	{
		// Has the cultist group reached the needed conversions?
		if (component.HasRisen)
			return 0;
		int conversionsUntilRise = component.ConversionsUntilRise - cultists.Count;
		conversionsUntilRise = (conversionsUntilRise > 0) ? conversionsUntilRise : 0;
		return conversionsUntilRise;
	}

	private void EmpowerCultists(List<EntityUid> cultists)
	{
		// Announce to everyone that the cult is growing stronger and then make eyes glow
		AnnounceToCultists(
			Loc.GetString("cult-ascend-1")+"\n",
			newlineNeeded:true
		);
		foreach (EntityUid cultist in cultists)
		{
			if (EntityManager.TryGetComponent(cultist, out AppearanceComponent? appearance))
			{
				// Only enable eyes if the body has an attached head
				var hasHead = false;
				if (TryComp<BodyComponent>(cultist, out var body))
				{
					var head = _body.GetBodyChildrenOfType(cultist, BodyPartType.Head, body).FirstOrDefault();
					hasHead = head.Id != EntityUid.Invalid;
				}
				_appearance.SetData(cultist, CultEyesVisuals.CultEyes, hasHead, appearance);
			}
		}
	}

	private void RiseCultists(List<EntityUid> cultists, bool announce = true)
	{
		if (announce)
		{
			// Announce to everyone that the cult is rising and then do the rising
			AnnounceToCultists(
				Loc.GetString("cult-ascend-2")+"\n",
				newlineNeeded:true
			);
		}
		foreach (EntityUid cultist in cultists)
		{
			// Ensure AppearanceComponent exists and set halo visual
			// This ensures halos are added even if the component is added later
			var appearance = EnsureComp<AppearanceComponent>(cultist);
			_appearance.SetData(cultist, CultHaloVisuals.CultHalo, true, appearance);
			UpdateCultHaloLight(cultist, true);
		}
	}

	/// <summary>
	/// Updates the point light for cultists with halos. Adds a bright red light with small radius when halo is active.
	/// </summary>
	private void UpdateCultHaloLight(EntityUid uid, bool hasHalo)
	{
		if (hasHalo)
		{
			var light = _pointLight.EnsureLight(uid);
			// Set enabled first to ensure the light is active
			_pointLight.SetEnabled(uid, true, light);
			// Then set the visual properties - make it bright and visible
			_pointLight.SetColor(uid, new Color(255, 0, 0), light); // Bright red
			_pointLight.SetEnergy(uid, 3.0f, light); // Bright
			_pointLight.SetRadius(uid, 1.0f, light); // Small but visible radius
		}
		else
		{
			// Remove the light if halo is disabled
			_pointLight.RemoveLightDeferred(uid);
		}
	}

	public void AnnounceStatus(BloodCultRuleComponent component, List<EntityUid> cultists, EntityUid? specificCultist = null)
	{
		List<EntityUid> constructs = new List<EntityUid>();
		var constructsQuery = AllEntityQuery<BloodCultConstructComponent, MobStateComponent>();
        while (constructsQuery.MoveNext(out var uid, out var _, out _))
        {
			if (_mobSystem.IsAlive(uid))
            	constructs.Add(uid);
        }
		if (component.CultistsWin)
		{
			if (specificCultist != null)
				AnnounceToCultist("Feed me.\n",
						(EntityUid)specificCultist, color:new Color(111, 80, 143, 255), fontSize:24, newlineNeeded:true);
			else
				AnnounceToCultists("Feed me.\n",
					color:new Color(111, 80, 143, 255), fontSize:24, newlineNeeded:true);
			return;
		}
		string purpleMessage;
		if (!component.VeilWeakened)
		{
			purpleMessage = Loc.GetString("cult-status-veil-strong");
		}
		else if (component.BloodAnomalySpawned)
		{
			purpleMessage = Loc.GetString("cult-status-veil-weak-anomaly");
		}
		else if (component.BloodAnomalySpawnScheduled)
		{
			purpleMessage = Loc.GetString("cult-status-veil-weak-pending");
		}
		else
		{
			purpleMessage = Loc.GetString("cult-status-veil-weak");
		}

		if (component.VeilWeakened && component.LocationForSummon != null)
		{
			var summonLocation = (WeakVeilLocation) component.LocationForSummon;
			purpleMessage += "\n" + Loc.GetString("cult-status-veil-weak-rift-location",
				("location", summonLocation.Name));

			if (specificCultist != null)
				purpleMessage += "\n" + Loc.GetString("cult-blood-progress-final-summon-location",
					("location", summonLocation.Name));
		}
		if (specificCultist != null)
			AnnounceToCultist(purpleMessage,
					(EntityUid)specificCultist, color:new Color(111, 80, 143, 255), fontSize:12, newlineNeeded:true);
		else
			AnnounceToCultists(purpleMessage,
					color:new Color(111, 80, 143, 255), fontSize:12, newlineNeeded:true);

	var totalCultists = cultists.Count;
	var totalConstructs = constructs.Count;

	var cultistLine = Loc.GetString("cult-status-cultdata",
		("cultMembers", totalCultists),
		("constructCount", totalConstructs));

	if (specificCultist != null)
		AnnounceToCultist(cultistLine,
			(EntityUid)specificCultist, fontSize: 11, newlineNeeded:true);
	else
		AnnounceToCultists(cultistLine,
			fontSize: 11, newlineNeeded:true);

	// Display blood collection progress
	string bloodMessage = GetBloodProgressMessage(component);
	if (specificCultist != null)
		AnnounceToCultist(bloodMessage, (EntityUid)specificCultist, color: new Color(139, 0, 0, 255), fontSize: 11, newlineNeeded: true);
	else
		AnnounceToCultists(bloodMessage, color: new Color(139, 0, 0, 255), fontSize: 11, newlineNeeded: true);
	}

	private string GetBloodProgressMessage(BloodCultRuleComponent component)
	{
		double currentBlood = component.BloodCollected;
		string currentPhase = "";
		double nextThreshold = 0.0;
		double bloodNeeded = 0.0;

		// Determine current phase and next threshold
		if (!component.HasEyes)
		{
			currentPhase = "Eyes";
			nextThreshold = component.BloodRequiredForEyes;
			bloodNeeded = Math.Max(0.0, nextThreshold - currentBlood);
		}
		else if (!component.HasRisen)
		{
			currentPhase = "Rise";
			nextThreshold = component.BloodRequiredForRise;
			bloodNeeded = Math.Max(0.0, nextThreshold - currentBlood);
		}
		else if (!component.VeilWeakened)
		{
			// Stage 2 complete - need to do Tear Veil ritual
			currentPhase = "Rise";
			nextThreshold = component.BloodRequiredForRise;
			bloodNeeded = 0.0; // Stage is complete
			
			string message = Loc.GetString("cult-blood-progress",
				("bloodCollected", Math.Round(currentBlood, 1)),
				("bloodNeeded", Math.Round(bloodNeeded, 1)),
				("nextPhase", currentPhase),
				("totalRequired", Math.Round(nextThreshold, 1)),
				("isComplete", true));
			
			// Show Tear Veil locations if they exist
			if (component.WeakVeil1 != null && component.WeakVeil2 != null && component.WeakVeil3 != null)
			{
				string name1 = ((WeakVeilLocation)component.WeakVeil1).Name;
				string name2 = ((WeakVeilLocation)component.WeakVeil2).Name;
				string name3 = ((WeakVeilLocation)component.WeakVeil3).Name;
				message += "\n" + Loc.GetString("cult-blood-progress-tear-veil",
					("location1", name1),
					("location2", name2),
					("location3", name3),
					("required", component.MinimumCultistsForVeilRitual));
			}
			
			return message;
		}
		else
		{
			// Stage 3 - Veil is weakened, need to do final summoning
			currentPhase = "Veil";
			nextThreshold = component.BloodRequiredForVeil;
			bloodNeeded = 0.0; // Stage is complete
			
			string message = Loc.GetString("cult-blood-progress",
				("bloodCollected", Math.Round(currentBlood, 1)),
				("bloodNeeded", Math.Round(bloodNeeded, 1)),
				("nextPhase", currentPhase),
				("totalRequired", Math.Round(nextThreshold, 1)),
				("isComplete", true));
			message += "\n" + Loc.GetString(
				component.BloodAnomalySpawned
					? "cult-blood-progress-final-summon-ready"
					: component.BloodAnomalySpawnScheduled
						? "cult-blood-progress-final-summon-pending"
						: "cult-blood-progress-final-summon");

			if (component.LocationForSummon != null)
			{
				message += "\n" + Loc.GetString("cult-blood-progress-final-summon-location",
					("location", ((WeakVeilLocation)component.LocationForSummon).Name));
			}
			
			return message;
		}

		bool isComplete = bloodNeeded <= 0.05; // Account for rounding precision
		return Loc.GetString("cult-blood-progress",
			("bloodCollected", Math.Round(currentBlood, 1)),
			("bloodNeeded", Math.Round(bloodNeeded, 1)),
			("nextPhase", currentPhase),
			("totalRequired", Math.Round(nextThreshold, 1)),
			("isComplete", isComplete));
	}

	// private bool TryGetRiftDirectionMessage(EntityUid cultistUid, WeakVeilLocation location, out string message)
	// {
	// 	message = Loc.GetString("cult-blood-progress-final-summon-location",
	// 		("location", location.Name));
	// 	return true;
	// }

	public void DistributeCommune(BloodCultRuleComponent component, string message, EntityUid sender)
	{
		string formattedMessage = FormattedMessage.EscapeText(message);

		EntityUid? mindId = CompOrNull<MindContainerComponent>(sender)?.Mind;

		if (mindId != null)
		{
			var metaData = MetaData(sender);
			string localSpeech;
			
			// Check if sender is a juggernaut - use juggernaut accent words instead of random chant
			if (HasComp<JuggernautComponent>(sender))
			{
				// Dynamically get the count of juggernaut accent words from the prototype
				var juggernautWordCount = 1; // Default to 1 if prototype not found
				if (_proto.TryIndex<ReplacementAccentPrototype>(JuggernautAccentPrototypeId, out var juggernautAccent) &&
				    juggernautAccent.FullReplacements != null && juggernautAccent.FullReplacements.Length > 0)
				{
					juggernautWordCount = juggernautAccent.FullReplacements.Length;
				}
				
				// Pick a random juggernaut accent word (1-based index)
				var juggernautWordIndex = _random.Next(1, juggernautWordCount + 1);
				localSpeech = Loc.GetString($"accent-words-juggernaut-{juggernautWordIndex}");
			}
			else
			{
				// Generate a random single-word chant from cult-chants.ftl
				localSpeech = GenerateChant(wordCount: 1);
			}
			
			_chat.TrySendInGameICMessage(sender, localSpeech, InGameICChatType.Whisper, ChatTransmitRange.Normal);
			_jobs.MindTryGetJob(mindId, out var prototype);
			string job = "Crewmember";
			if (prototype != null)
				job = prototype.LocalizedName;
			AnnounceToCultists(message = Loc.GetString("cult-commune-message", ("name", metaData.EntityName),
				("job", job), ("message", formattedMessage)), color:new Color(166, 27, 27, 255),
				fontSize: 12, newlineNeeded:false, includeGhosts:true);
		}
	}

	/// <summary>
	/// Adds blood to the ritual pool when someone is converted.
	/// Caps blood at the current stage threshold to prevent over-collection.
	/// </summary>
	public void AddBloodForConversion(double amount = 100.0)
	{
		var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
		while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
		{
			// Determine the current stage cap
			double currentCap = ruleComp.BloodRequiredForVeil;
			if (!ruleComp.HasEyes)
				currentCap = ruleComp.BloodRequiredForEyes;
			else if (!ruleComp.HasRisen)
				currentCap = ruleComp.BloodRequiredForRise;
			else if (!ruleComp.VeilWeakened)
				currentCap = ruleComp.BloodRequiredForVeil;
			
			// Add blood but don't exceed the current stage cap
			ruleComp.BloodCollected = Math.Min(ruleComp.BloodCollected + amount, currentCap);
			// BloodCultRuleComponent is server-only and doesn't need to be dirtied
			return;
		}
	}

	/// <summary>
	/// Progresses the cult to stage 3 (Veil Weakened) when the Tear the Veil ritual is completed.
	/// Sets up the final summoning ritual site.
	/// </summary>
	public void CompleteVeilRitual()
	{
		var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
		while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
		{
			if (!ruleComp.HasRisen)
				return; // Can't complete the ritual before reaching stage 2

			if (!ruleComp.VeilWeakened)
			{
				ruleComp.VeilWeakened = true;
				// Get all cultists (constructs don't have the culthalo sprite layer, so they're excluded)
				var cultists = GetCultists();
				RiseCultists(cultists, announce: false);

				ruleComp.BloodAnomalySpawnScheduled = true;
				ruleComp.BloodAnomalySpawned = false;
				ruleComp.BloodAnomalySpawnTime = _timing.CurTime + TimeSpan.FromMinutes(2);
				ruleComp.BloodAnomalyUid = null;

				// Announcement will be handled in ActiveTick
				AnnounceStatus(ruleComp, cultists);
				ruleComp.VeilWeakenedAnnouncementPlayed = true; // Prevent duplicate announcement in ActiveTick
			}
			return;
		}
	}

	/// <summary>
	/// Sets the win condition when Nar'Sie is summoned.
	/// </summary>
	public void AnnounceNarsieSummon()
	{
		var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
		while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
		{
			ruleComp.CultistsWin = true;
			ruleComp.CultVictoryEndTime = _timing.CurTime + ruleComp.CultVictoryEndDelay;

			return;
		}
	}

	#region Admin Commands

	[AdminCommand(AdminFlags.Fun)]
	private void QueryBloodCommand(IConsoleShell shell, string argstr, string[] args)
	{
		var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
		var found = false;

		while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
		{
			if (!GameTicker.IsGameRuleActive(uid, gameRule))
				continue;

			found = true;
			var currentBlood = Math.Round(ruleComp.BloodCollected, 1);
			var eyesRequired = Math.Round(ruleComp.BloodRequiredForEyes, 1);
			var riseRequired = Math.Round(ruleComp.BloodRequiredForRise, 1);
			var veilRequired = Math.Round(ruleComp.BloodRequiredForVeil, 1);

			shell.WriteLine($"=== Blood Cult Status ===");
			shell.WriteLine($"Current Blood Collected: {currentBlood}u");
			shell.WriteLine($"");
			shell.WriteLine($"Phase 1 (Eyes): {eyesRequired}u needed - {(ruleComp.HasEyes ? "COMPLETE" : $"{Math.Round(eyesRequired - currentBlood, 1)}u remaining")}");
			shell.WriteLine($"Phase 2 (Rise): {riseRequired}u needed - {(ruleComp.HasRisen ? "COMPLETE" : $"{Math.Round(riseRequired - currentBlood, 1)}u remaining")}");
			shell.WriteLine($"Phase 3 (Veil): {veilRequired}u needed - {(ruleComp.VeilWeakened ? "COMPLETE" : $"{Math.Round(veilRequired - currentBlood, 1)}u remaining")}");

			if (shell.Player != null)
			{
				_adminLogger.Add(LogType.Action, LogImpact.Low, 
					$"{shell.Player} queried blood cult status: {currentBlood}u collected");
			}
		}

		if (!found)
		{
			shell.WriteError("No active Blood Cult game rule found.");
		}
	}

	[AdminCommand(AdminFlags.Fun)]
	private void SetBloodCommand(IConsoleShell shell, string argstr, string[] args)
	{
		if (args.Length != 1)
		{
			shell.WriteError("Usage: cult_setblood <amount>");
			return;
		}

		if (!double.TryParse(args[0], out var amount))
		{
			shell.WriteError("Invalid amount. Must be a number.");
			return;
		}

		if (amount < 0)
		{
			shell.WriteError("Amount cannot be negative.");
			return;
		}

		var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
		var found = false;

		while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
		{
			if (!GameTicker.IsGameRuleActive(uid, gameRule))
				continue;

			found = true;
			var oldAmount = ruleComp.BloodCollected;
			ruleComp.BloodCollected = amount;

			shell.WriteLine($"Blood cult amount set from {Math.Round(oldAmount, 1)}u to {Math.Round(amount, 1)}u");

			if (shell.Player != null)
			{
				_adminLogger.Add(LogType.Action, LogImpact.Medium, 
					$"{shell.Player} set blood cult amount from {oldAmount}u to {amount}u");
			}
		}

		if (!found)
		{
			shell.WriteError("No active Blood Cult game rule found.");
		}
	}

	#endregion
}
