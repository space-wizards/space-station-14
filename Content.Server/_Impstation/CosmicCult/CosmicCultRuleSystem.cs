using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.GameTicking.Rules;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Roles;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Content.Server.Radio.Components;
using Robust.Shared.Player;
using Content.Server.EUI;
using Robust.Shared.Random;
using Content.Server.Announcements.Systems;
using Robust.Server.Audio;
using Content.Shared.Coordinates;
using Content.Shared.Parallax;
using Robust.Shared.Map.Components;
using Content.Shared.Temperature.Components;
using Content.Server.Body.Components;
using Content.Server.Atmos.Components;
using Content.Server.Objectives.Components;
using Robust.Server.Player;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Stunnable;
using Content.Shared.Mind;
using Content.Shared.Administration.Logs;
using Content.Server.Actions;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using System.Linq;
using Content.Server.Shuttles.Systems;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Body.Systems;
using Content.Server.RoundEnd;
using Content.Server.Audio;
using Content.Shared.Audio;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage;
using Content.Server.Bible.Components;
using Content.Shared.UserInterface;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult;

/// <summary>
/// Where all the main stuff for Cosmic Cultists happens.
/// </summary>
public sealed class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly AnnouncerSystem _announce = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_briefing.ogg");
    public readonly SoundSpecifier DeconvertSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_deconvert.ogg");
    public Entity<MonumentComponent> MonumentInGame; // the monument in the current round.
    public int CurrentTier; // current cult tier
    public int TotalCrew; // total connected players
    public int TotalCult; // total cultists
    public double PercentConverted; // percentage of connected players that are cultists
    public double Tier3NumCrew; // 40 percent of connected players
    public int EntropySiphoned; // the total entropy siphoned by the cult.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CosmicCultComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<CosmicMarkGodComponent, ComponentInit>(OnGodSpawn);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        // Reset the cult data to defaults.
        EntropySiphoned = 0;
        CurrentTier = 0;
        TotalCrew = 0;
        TotalCult = 0;
        PercentConverted = 0;
        Tier3NumCrew = 40;
    }

    private void OnAntagSelect(Entity<CosmicCultRuleComponent> uid, ref AfterAntagEntitySelectedEvent args)
    {
        TryStartCult(args.EntityUid, uid);
    }

    #region Round & Objectives

    private void OnGodSpawn(Entity<CosmicMarkGodComponent> uid, ref ComponentInit args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var ruleUid, out _, out var cultRule, out _))
        {
            SetWinType((ruleUid, cultRule), WinType.CultComplete); //Last i checked, there's no coming back from summoning a fragment of raw cosmic power. Cult wins this round.
            _roundEnd.EndRound(); //Woo game over yeaaaah
            foreach (var cultist in cultRule.Cultists)
            {
                if (TryComp<MobStateComponent>(cultist, out var state) && state.CurrentState != MobState.Dead)
                {
                    if (!TryComp<MindContainerComponent>(cultist, out var mindContainer) || !mindContainer.HasMind)
                        return;
                    if (!EntityManager.TryGetComponent<MetaDataComponent>(cultist, out var metaData))
                        return;

                    var ascendant = Spawn("MobCosmicAstralAscended", Transform(cultist).Coordinates);
                    _mind.TransferTo(mindContainer.Mind.Value, ascendant);
                    _metaData.SetEntityName(ascendant, Loc.GetString("cosmiccult-astral-ascendant", ("NAME", metaData.EntityName))); //Renames cultists' ascendant forms to "[CharacterName], Ascendant"
                    _body.GibBody(cultist); //You won't be needing your old body anymore, so let's explode it to enhance the vibes.
                }
            }
        }
    }

    private static void SetWinType(Entity<CosmicCultRuleComponent> uid, WinType type)
    {
        if (uid.Comp.WinLocked)
            return;
        uid.Comp.WinType = type;

        if (type is WinType.CultComplete or WinType.CrewComplete or WinType.CultMajor) //Let's lock in our WinType to prevent us from setting a worse win if a better win's been achieved.
            uid.Comp.WinLocked = true;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New is not GameRunLevel.PostRound) //Are we moving to post-round?
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var cultRule, out _))
        {
            OnRoundEnd((uid, cultRule)); //If so, let's consult our Winconditions and set an appropriate WinType.
        }
    }

    private void OnRoundEnd(Entity<CosmicCultRuleComponent> uid)
    {
        var tier = CurrentTier;
        var leaderAlive = false;
        var centcomm = _emergency.GetCentcommMaps();
        var wrapup = AllEntityQuery<CosmicCultComponent, TransformComponent>();
        while (wrapup.MoveNext(out var cultist, out _, out var cultistLocation))
        {
            if (cultistLocation.MapUid != null && centcomm.Contains(cultistLocation.MapUid.Value))
            {
                if (HasComp<CosmicCultLeadComponent>(cultist))
                    leaderAlive = true;
            }
        }
        if (tier < 3 && leaderAlive)
            SetWinType(uid, WinType.Neutral); //The Monument isn't Tier 3, but the cult leader's alive and at Centcomm! That's a Neutral outcome.
        var monument = AllEntityQuery<CosmicFinaleComponent>();
        while (monument.MoveNext(out var monumentUid, out var comp))
        {
            _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);
            if (comp.FinaleActive || comp.BufferComplete || comp.FinaleReady)
            {
                SetWinType(uid, WinType.CultMajor); //Despite the crew's escape, The Finale is available or active. Major win!
            }
            else if (tier == 3)
            {
                SetWinType(uid, WinType.CultMinor); //The crew escaped, and The Monument wasn't fully empowered. Good enough for a small win!
            }
            QueueDel(monumentUid); // The monument doesn't need to stick around postround! Into the bin with you.
        }

        var cultistsAlive = EntityQuery<CosmicCultComponent, MobStateComponent>(true)
            .Any(op => op.Item2.CurrentState == MobState.Alive && op.Item1.Running);
        if (cultistsAlive)
            return; // There's still cultists alive! We return, since this leads to the default CrewMinor WinType.

        if (TotalCult == 0)
            SetWinType(uid, WinType.CrewComplete); // No cultists registered! That means everyone got deconverted!
        else
            SetWinType(uid, WinType.CrewMajor); // There's still cultists registered, but if we got here, that means they're all dead. Rip.
    }

    protected override void AppendRoundEndText(EntityUid uid,
        CosmicCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var winType = Loc.GetString($"cosmiccult-roundend-{component.WinType.ToString().ToLower()}");
        var summaryText = Loc.GetString($"cosmiccult-summary-{component.WinType.ToString().ToLower()}");
        args.AddLine(winType);
        args.AddLine(summaryText);
        args.AddLine(Loc.GetString("cosmiccult-roundend-cultist-count", ("initialCount", TotalCult)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-cultpop-count", ("count", PercentConverted)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-entropy-count", ("count", EntropySiphoned)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-monument-stage", ("stage", CurrentTier)));
    }
    public void IncrementCultObjectiveEntropy(Entity<CosmicCultComponent> uid)
    {
        EntropySiphoned += uid.Comp.CosmicSiphonQuantity;
        var query = EntityQueryEnumerator<CosmicEntropyConditionComponent>();
        while (query.MoveNext(out var _, out var entropyComp))
        {
            entropyComp.Siphoned = EntropySiphoned;
        }
    }
    #endregion

    #region Monument
    public void UpdateMonumentAppearance(Entity<MonumentComponent> uid, bool tierUp) // this is kinda awful, but it works, and i've seen worse. improve it at thine leisure
    {
        if (!TryComp<CosmicFinaleComponent>(uid, out var finaleComp))
            return;
        _appearance.SetData(uid, MonumentVisuals.Monument, CurrentTier);

        switch (CurrentTier)
        {
            case 3:
                _appearance.SetData(uid, MonumentVisuals.Tier3, true);
                break;
            case 2:
                _appearance.SetData(uid, MonumentVisuals.Tier3, false);
                break;
        }

        if (tierUp)
        {
            var transformComp = EnsureComp<MonumentTransformingComponent>(uid);
            transformComp.EndTime = _timing.CurTime + uid.Comp.TransformTime;
            _appearance.SetData(uid, MonumentVisuals.Transforming, true);
        }

        if (finaleComp.FinaleReady || finaleComp.FinaleActive)
            _appearance.SetData(uid, MonumentVisuals.FinaleReached, true);
    }

    public void UpdateCultData(Entity<MonumentComponent> uid) // This runs every time Entropy is Inserted into The Monument, and every time a Cultist is Converted or Deconverted.
    {
        var scalar = 7; // one cultist is worth this many entropy
        //todo this should probably be a cvar - ruddygreat

        if (uid.Comp == null || !TryComp<CosmicFinaleComponent>(uid, out var finaleComp))
            return;

        TotalCrew = _antag.GetTotalPlayerCount(_playerMan.Sessions);

#if DEBUG
        if (TotalCrew < 25)
            TotalCrew = 25;
#endif

        PercentConverted = Math.Round((double)(100 * TotalCult) / TotalCrew);
        Tier3NumCrew = Math.Round((double)TotalCrew / 100 * 40); // 40% of current pop
        //todo make the 40% there a cvar - ruddygreat

        //note - these ar the thresholds for moving to the next tier
        //so t1 -> 2 needs 20% of the crew
        //t2 -> 3 needs 40%
        //and t3 -> finale needs an extra 20 entropy
        switch (CurrentTier)
        {
            case 1:
                uid.Comp.ProgressOffset = 0;
                uid.Comp.TargetProgress = (int) (Tier3NumCrew / 2 * scalar);
                break;
            case 2:
                uid.Comp.ProgressOffset = (float) (Tier3NumCrew / 2 * scalar); //reset the progress offset
                uid.Comp.TargetProgress = (int) (Tier3NumCrew * scalar);
                break;
            case 3:
                uid.Comp.ProgressOffset = (float) (Tier3NumCrew * scalar);
                uid.Comp.TargetProgress = (int) Tier3NumCrew * scalar + 20;
                break;
        }

        uid.Comp.CurrentProgress = uid.Comp.TotalEntropy + TotalCult * scalar;

        if (uid.Comp.CurrentProgress >= uid.Comp.TargetProgress && CurrentTier == 3 && !finaleComp.FinaleActive && !finaleComp.FinaleReady)
            FinaleReady(uid, finaleComp);
        else if (finaleComp.FinaleReady || finaleComp.FinaleActive)
            uid.Comp.CurrentProgress = uid.Comp.TargetProgress;
        else if (uid.Comp.CurrentProgress >= uid.Comp.TargetProgress && CurrentTier == 2)
            MonumentTier3(uid);
        else if (uid.Comp.CurrentProgress >= uid.Comp.TargetProgress && CurrentTier == 1)
            MonumentTier2(uid);

        UpdateMonumentAppearance(uid, false);

        Dirty(uid.Owner, uid.Comp);
        _ui.SetUiState(uid.Owner, MonumentKey.Key, new MonumentBuiState(uid.Comp));
    }

    public void MonumentTier1(Entity<MonumentComponent> uid)
    {
        CurrentTier = 1;
        UpdateMonumentAppearance(uid, false);
        MonumentInGame = uid; //Since there's only one Monument per round, let's store its UID for the rest of the round. Saves us on spamming enumerators.

        //this is probably unnecessary but I have no idea where they get added to the list atm - ruddygreat
        foreach (var glyphProto in _protoMan.EnumeratePrototypes<GlyphPrototype>().Where(proto => proto.Tier == 1))
        {
            uid.Comp.UnlockedGlyphs.Add(glyphProto.ID);
        }

        //basically completely unnecessary, but putting this here for sanity & futureproofing - ruddygreat
        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var cultComp))
        {

            foreach (var influenceProto in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == 1))
            {
                cultComp.UnlockedInfluences.Add(influenceProto.ID);
            }

            Dirty(cultist, cultComp);
        }

        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out _, out var objectiveComp))
        {
            objectiveComp.Tier = 1;
        }

        Dirty(uid);
        _ui.SetUiState(uid.Owner, MonumentKey.Key, new MonumentBuiState(uid.Comp));
    }

    private void MonumentTier2(Entity<MonumentComponent> uid)
    {
        //uid.Comp.PercentageComplete = 50;
        CurrentTier = 2;

        foreach (var glyphProto in _protoMan.EnumeratePrototypes<GlyphPrototype>().Where(proto => proto.Tier == 2))
        {
            uid.Comp.UnlockedGlyphs.Add(glyphProto.ID);
        }

        UpdateMonumentAppearance(uid, true);

        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var cultComp))
        {

            foreach (var influenceProto in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == 2))
            {
                cultComp.UnlockedInfluences.Add(influenceProto.ID);
            }

            cultComp.EntropyBudget += (int) Math.Floor(Math.Round((double)TotalCrew / 100 * 4)); // pity system. 4% of the playercount worth of entropy on tier up
            Dirty(cultist, cultComp);
        }

        var sender = Loc.GetString("cosmiccult-announcement-sender");
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier2-progress"), sender, Color.FromHex("#4cabb3"));
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier2-warning"), null, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal("/Audio/_Impstation/CosmicCult/tier2.ogg", Filter.Broadcast(), false, AudioParams.Default);

        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out _, out var objectiveComp))
        {
            objectiveComp.Tier = 2;
        }

        for (var i = 0; i < Convert.ToInt16(TotalCrew / 4); i++) // spawn # malign rifts equal to 25% of the playercount
        {
            if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
            {
                Spawn("CosmicMalignRift", coords);
            }
        }

        var lights = EntityQueryEnumerator<PoweredLightComponent>();
        while (lights.MoveNext(out var light, out _))
        {
            if (!_rand.Prob(0.50f))
                continue;
            _ghost.DoGhostBooEvent(light);
        }

        Dirty(uid);
        _ui.SetUiState(uid.Owner, MonumentKey.Key, new MonumentBuiState(uid.Comp));
    }

    private void MonumentTier3(Entity<MonumentComponent> uid)
    {
        //uid.Comp.PercentageComplete = 0;
        uid.Comp.HasCollision = true;
        CurrentTier = 3;

        _visibility.SetLayer(uid.Owner, 1, true);

        foreach (var glyphProto in _protoMan.EnumeratePrototypes<GlyphPrototype>().Where(proto => proto.Tier == 3))
        {
            uid.Comp.UnlockedGlyphs.Add(glyphProto.ID);
        }

        UpdateMonumentAppearance(uid, true);

        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var cultComp))
        {
            EnsureComp<CosmicStarMarkComponent>(cultist);
            EnsureComp<PressureImmunityComponent>(cultist);
            RemComp<TemperatureSpeedComponent>(cultist);
            RemComp<RespiratorComponent>(cultist);

            _damage.SetDamageContainerID(cultist, "BiologicalMetaphysical");

            foreach (var influenceProto in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == 3))
            {
                cultComp.UnlockedInfluences.Add(influenceProto.ID);
            }

            cultComp.EntropyBudget += Convert.ToInt16(Math.Floor(Math.Round((double)TotalCrew / 100 * 4))); //pity system. 4% of the playercount worth of entropy on tier up
            Dirty(cultist, cultComp);
        }

        var sender = Loc.GetString("cosmiccult-announcement-sender");
        var mapData = _map.GetMap(_transform.GetMapId(uid.Owner.ToCoordinates()));
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier3-progress"), sender, Color.FromHex("#4cabb3"));
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier3-warning"), null, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal("/Audio/_Impstation/CosmicCult/tier3.ogg", Filter.Broadcast(), false, AudioParams.Default);

        EnsureComp<ParallaxComponent>(mapData, out var parallax);
        parallax.Parallax = "CosmicFinaleParallax";
        Dirty(mapData, parallax);

        EnsureComp<MapLightComponent>(mapData, out var mapLight);
        mapLight.AmbientLightColor = Color.FromHex("#210746");

        Dirty(mapData, mapLight);

        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out var _, out var objectiveComp))
        {
            objectiveComp.Tier = 3;
        }

        var lights = EntityQueryEnumerator<PoweredLightComponent>();
        while (lights.MoveNext(out var light, out _))
        {
            if (!_rand.Prob(0.25f))
                continue;
            _ghost.DoGhostBooEvent(light);
        }

        Dirty(uid);
        _ui.SetUiState(uid.Owner, MonumentKey.Key, new MonumentBuiState(uid.Comp));
    }

    private void FinaleReady(Entity<MonumentComponent> uid, CosmicFinaleComponent finaleComp)
    {
        if (TryComp<CosmicCorruptingComponent>(uid, out var comp))
            comp.Enabled = true;

        if (TryComp<ActivatableUIComponent>(uid, out var uiComp))
        {
            if (TryComp<UserInterfaceComponent>(uid, out var uiComp2)) //close the UI for everyone who has it open
            {
                _ui.CloseUi((uid.Owner, uiComp2), MonumentKey.Key);
            }

            uiComp.Key = null;
        }

        finaleComp.FinaleReady = true;
        uid.Comp.Enabled = false;

        Dirty(uid);
        _ui.SetUiState(uid.Owner, MonumentKey.Key, new MonumentBuiState(uid.Comp));

        _popup.PopupCoordinates(Loc.GetString("cosmiccult-finale-ready"), Transform(uid).Coordinates, PopupType.Large);
    }
    #endregion

    #region De- & Conversion
    public void TryStartCult(EntityUid uid, Entity<CosmicCultRuleComponent> rule)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        EnsureComp<CosmicCultComponent>(uid, out var cultComp);
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);

        _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);

        if (cosmicRole is not null)
        {
            EnsureComp<RoleBriefingComponent>(cosmicRole.Value.Owner);
            if (HasComp<CosmicCultLeadComponent>(cosmicRole.Value.Owner))
                Comp<RoleBriefingComponent>(cosmicRole.Value.Owner).Briefing = Loc.GetString("objective-cosmiccultlead-charactermenu");
            else
                Comp<RoleBriefingComponent>(cosmicRole.Value.Owner).Briefing = Loc.GetString("objective-cosmiccult-charactermenu");
        }

        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-roundstart-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels.Add("CosmicRadio");
        transmitter.Channels.Add("CosmicRadio");

        if (_mind.TryGetSession(mindId, out var session))
        {
            _euiMan.OpenEui(new CosmicRoundStartEui(), session);
        }

        TotalCult++;

        cultComp.StoredDamageContainer = Comp<DamageableComponent>(uid).DamageContainerID!.Value; //todo: nullable

        Dirty(uid, cultComp);

        rule.Comp.Cultists.Add(uid);
    }

    public void CosmicConversion(EntityUid uid)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var _, out _, out var cosmicGamerule, out _))
        {
            if (!_mind.TryGetMind(uid, out var mindId, out var mind))
                return;

            _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
            _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);

            if (cosmicRole is not null)
            {
                EnsureComp<RoleBriefingComponent>(cosmicRole.Value.Owner);
                Comp<RoleBriefingComponent>(cosmicRole.Value.Owner).Briefing = Loc.GetString("objective-cosmiccult-charactermenu");
            }

            _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-conversion-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
            _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

            var cultComp = EnsureComp<CosmicCultComponent>(uid);
            cultComp.StoredDamageContainer = Comp<DamageableComponent>(uid).DamageContainerID!.Value;
            EnsureComp<IntrinsicRadioReceiverComponent>(uid);

            if (CurrentTier == 3)
            {
                _damage.SetDamageContainerID(uid, "BiologicalMetaphysical");
                cultComp.EntropyBudget = 20; // pity balance

                foreach (var influenceProto in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == 3))
                {
                    cultComp.UnlockedInfluences.Add(influenceProto.ID);
                }

                EnsureComp<CosmicStarMarkComponent>(uid);
                EnsureComp<PressureImmunityComponent>(uid);
                EnsureComp<TemperatureImmunityComponent>(uid);
                RemComp<RespiratorComponent>(uid);
            }
            else if (CurrentTier == 2)
            {
                cultComp.EntropyBudget = 12; // pity balance

                foreach (var influenceProto in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == 2))
                {
                    cultComp.UnlockedInfluences.Add(influenceProto.ID);
                }
            }

            Dirty(uid, cultComp);

            var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
            var radio = EnsureComp<ActiveRadioComponent>(uid);
            radio.Channels = new() { "CosmicRadio" };
            transmitter.Channels = new() { "CosmicRadio" };

            _mind.TryAddObjective(mindId, mind, "CosmicFinalityObjective");
            _mind.TryAddObjective(mindId, mind, "CosmicMonumentObjective");
            _mind.TryAddObjective(mindId, mind, "CosmicEntropyObjective");

            if (_mind.TryGetSession(mindId, out var session))
            {
                _euiMan.OpenEui(new CosmicConvertedEui(), session);
            }

            RemComp<BibleUserComponent>(uid);

            TotalCult++;
            cosmicGamerule.Cultists.Add(uid);

            UpdateCultData(MonumentInGame);
        }
    }
    private void OnComponentShutdown(Entity<CosmicCultComponent> uid, ref ComponentShutdown args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var _, out _, out var cosmicGamerule, out _))
        {
            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);
            foreach (var actionEnt in uid.Comp.ActionEntities) _actions.RemoveAction(actionEnt);

            if (TryComp<IntrinsicRadioTransmitterComponent>(uid, out var transmitter))
                transmitter.Channels.Remove("CosmicRadio");
            if (TryComp<ActiveRadioComponent>(uid, out var radio))
                radio.Channels.Remove("CosmicRadio");
            RemComp<CosmicCultLeadComponent>(uid);
            RemComp<InfluenceVitalityComponent>(uid);
            RemComp<InfluenceStrideComponent>(uid);
            RemComp<PressureImmunityComponent>(uid);
            RemComp<TemperatureImmunityComponent>(uid);
            RemComp<CosmicStarMarkComponent>(uid);
            _damage.SetDamageContainerID(uid, uid.Comp.StoredDamageContainer);
            _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-fluff"), Color.FromHex("#4cabb3"), DeconvertSound);
            _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-deconverted-briefing"), Color.FromHex("#cae8e8"), null);

            if (!_mind.TryGetMind(uid, out var mindId, out _) || !TryComp<MindComponent>(mindId, out var mindComp))
                return;

            _mind.ClearObjectives(mindId, mindComp); // LOAD-BEARING #imp function to remove all of someone's objectives, courtesy of TCRGDev(Github)
            _role.MindTryRemoveRole<CosmicCultRoleComponent>(mindId);
            _role.MindTryRemoveRole<RoleBriefingComponent>(mindId);
            if (_mind.TryGetSession(mindId, out var session))
            {
                _euiMan.OpenEui(new CosmicDeconvertedEui(), session);
            }
            _eye.SetVisibilityMask(uid, 1);
            TotalCult--;
            cosmicGamerule.Cultists.Remove(uid);
            UpdateCultData(MonumentInGame);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }
    #endregion
}
