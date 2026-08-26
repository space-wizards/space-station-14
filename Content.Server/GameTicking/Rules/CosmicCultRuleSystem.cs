using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Parallax;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Content.Shared.Weather;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;
using System.Collections.Immutable;
using System.Linq;
using Content.Server.Audio;
using Content.Server.CosmicCult;
using Content.Server.CosmicCult.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Pinpointer;
using Content.Shared.Audio;
using Content.Shared.Coordinates;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Abilities;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pinpointer;
using Content.Stellar.Server.CosmicCult.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private CosmicBreachSystem _breach = default!;
    [Dependency] private CosmicCultSystem _cosmicCult = default!;
    [Dependency] private EuiManager _euiMan = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private  IPlayerManager _playerMan = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedCosmicShiftSystem _cultShift = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StationSystem _station = default!;
    // [Dependency] private StellarGoalsSystem _goals = default!;
    // [Dependency] private StellarNumericGoalSystem _numericGoal = default!;

    private readonly SoundSpecifier _briefingSound = new SoundPathSpecifier("/Audio/Cosmic/cosmic-start.ogg");
    private readonly SoundSpecifier _finaleSound = new SoundPathSpecifier("/Audio/Cosmic/alarm-octarine.ogg");
    private readonly SoundSpecifier _tier3Sound = new SoundPathSpecifier("/Audio/Cosmic/tier3.ogg");
    private readonly SoundSpecifier _tier2Sound = new SoundPathSpecifier("/Audio/Cosmic/tier2.ogg");
    private static readonly EntProtoId MindRole = "MindRoleCosmicCultist";

    private HashSet<Entity<NavMapBeaconComponent>> _beaconSet = new();
    private HashSet<Entity<CosmicBreachComponent, TransformComponent>> _breachSet = new();

    private ISawmill _sawmill = default!;
    private TimeSpan _finaleTimeMax;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("cosmiccult");

        // Subs.CVar(_config, STCCVars.CosmicCultFinaleTargetTime, value => _finaleTimeMax = TimeSpan.FromMinutes(value), true);
    }

    #region Starting Events
    protected override void Added(EntityUid uid, CosmicCultRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        // component.GoalsContainer = _goals.SpawnContainer("Cosmic Cult");
        // var ok = _goals.TryAddGoals(component.GoalsContainer.Value, component.Goals);
        // Debug.Assert(ok);

        var station = _random.Pick(_station.GetStations());
        if (_station.GetLargestGrid(station) is not { } grid)
            return;

        component.StationGrid = grid;
        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, CosmicCultRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        component.TotalCrew = _playerMan.Sessions.Count(session => session.Status == SessionStatus.InGame && HasComp<HumanoidProfileComponent>(session.AttachedEntity));
        component.FinaleTimer = gameRule.ActivatedAt + _finaleTimeMax;
        component.Tier3Timer = gameRule.ActivatedAt + _finaleTimeMax * 0.7;
        component.Tier2Timer = gameRule.ActivatedAt + _finaleTimeMax * 0.4;

        _beaconSet.Clear();
        _breachSet.Clear();
        _lookup.GetChildEntities(component.StationGrid, _beaconSet);
        if (component.VoidMapId != null)
            _lookup.GetEntitiesOnMap(component.VoidMapId.Value, _breachSet);

        base.Started(uid, component, gameRule, args);
    }

    [SubscribeLocalEvent]
    private void OnRuleLoadedGrids(Entity<CosmicCultRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        ent.Comp.VoidMapId = args.Map;
    }

    [SubscribeLocalEvent]
    private void OnAntagSelect(Entity<CosmicCultRuleComponent> uid, ref AfterAntagEntitySelectedEvent args)
    {
        TryStartCult(args.EntityUid, uid);
    }
    #endregion

    private void SpawnRift()
    {
        if (TryFindRandomTile(out var _, out var _, out var _, out var coords)) { Spawn("CosmicMalignRift", coords); }
    }

    private void SpawnStigma()
    {
        if (TryFindRandomTile(out var _, out var _, out var _, out var coords)) { Spawn("CosmicEntropicStigmaSpawn", coords); }
    }

    public void UpdateCultData(Entity<CosmicCultRuleComponent> cult) // Runs every time Entropy is siphoned and whenever a crewmember is Converted.
    {
        if (!TryComp<GameRuleComponent>(cult, out var gameRule))
            return;

        cult.Comp.TotalCrew = _playerMan.Sessions.Count(session => session.Status == SessionStatus.InGame && HasComp<HumanoidProfileComponent>(session.AttachedEntity));

        #if DEBUG
        if (cult.Comp.TotalCrew < 25)
            cult.Comp.TotalCrew = 25;
        #endif

        var maxTime = gameRule.ActivatedAt + _finaleTimeMax;
        var minTime = gameRule.ActivatedAt + _finaleTimeMax * 0.33;
        var percentOutOfCrew = Math.Clamp(cult.Comp.PortionConverted / 0.45f, 0f, 1f);
        var entropyOutOfCrew = Math.Clamp(cult.Comp.EntropySiphoned / (cult.Comp.TotalCrew * 3f), 0f, 1f);
        var lerpTime = MathHelper.Lerp(maxTime, minTime, percentOutOfCrew * 0.4 + entropyOutOfCrew * 0.6);

        if (cult.Comp.FinaleTimer is not null)
            cult.Comp.FinaleTimer = lerpTime;
        if (cult.Comp.Tier3Timer is not null)
            cult.Comp.Tier3Timer = lerpTime * 0.8;
        if (cult.Comp.Tier2Timer is not null)
            cult.Comp.Tier2Timer = lerpTime * 0.4;
    }

    #region Active Ticking
    protected override void ActiveTick(EntityUid uid, CosmicCultRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (component.RiftTimer is { } riftTimer && _timing.CurTime >= riftTimer)
        {
            component.RiftTimer = _timing.CurTime + _random.Next(TimeSpan.FromSeconds(230), TimeSpan.FromSeconds(360)); // 3min50 to 6min between new rifts.
            SpawnRift();
        }

        if (component.StigmaTimer is { } stigmaTimer && _timing.CurTime >= stigmaTimer)
        {
            component.StigmaTimer = _timing.CurTime + _random.Next(TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(200)); // 2min to 3min 20sec between new stigma.
            SpawnStigma();
        }

        // if (component.BreachTimer is { } breachTimer && _timing.CurTime >= breachTimer)
        //     DoBreach(uid, component);
        //
        // if (component.CultWinTimer is { } winTimer && _timing.CurTime >= winTimer)
        //     CultWin(uid, component);
        //
        // if (component.FinaleTimer is { } finaleTimer && _timing.CurTime >= finaleTimer)
        //     StartFinale(uid, component);
        //
        // // Just to make sure nobody gets stuck station-side, 5 seconds before the finale, we cancel all doAfters on cultists and strip their ability to Shift.
        // if (component.FinaleTimer is { } finaleSetup && !component.FinaleSetup && _timing.CurTime >= (finaleSetup - TimeSpan.FromSeconds(5)))
        //     FinaleSetup(uid, component);
        //
        // if (component.Tier3Timer is { } tier3Timer && _timing.CurTime >= tier3Timer)
        //     StartTier3(uid, component);
        //
        // if (component.Tier2Timer is { } tier2Timer && _timing.CurTime >= tier2Timer)
        //     StartTier2(uid, component);
    }

    private void DoBreach(EntityUid uid, CosmicCultRuleComponent component)
    {
        if (_beaconSet.Count == 0 || _breachSet.Count == 0)
            component.BreachTimer = null;
        else
        {
            component.BreachTimer = _timing.CurTime + TimeSpan.FromSeconds(20); // New station-side breach every 20 seconds until all breaches have been linked up.
            var stationBreach = _breach.StationBreach(_beaconSet);
            var cosmicBreach = _random.Pick(_breachSet);

            if (stationBreach != null)
            {
                cosmicBreach.Comp1.LinkedBreach = stationBreach;
                stationBreach.Value.Comp.LinkedBreach = cosmicBreach;
                _breachSet.Remove(cosmicBreach);
                Spawn("CosmicBreachSpawnEffect", Transform(stationBreach.Value).Coordinates);

                var indicatedLocation = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((stationBreach.Value, Transform(stationBreach.Value))));
                _chatSystem.DispatchStationAnnouncement(stationBreach.Value, Loc.GetString("cosmiccult-announce-breach-location", ("location", indicatedLocation)), null, false, null, Color.FromHex("#cae8e8"));
            }
        }
    }

    private void CultWin(EntityUid uid, CosmicCultRuleComponent component)
    {
        component.CultWinTimer = null;
        component.CultWin = true;
        AdjustCultObjectiveFinality(1);
        _sound.StopStationEventMusic(component.StationGrid, StationEventMusicType.CosmicCult);
        _roundEnd.EndRound();
    }

    private void StartFinale(EntityUid uid, CosmicCultRuleComponent component)
    {
        component.CultWinTimer = _timing.CurTime + TimeSpan.FromSeconds(364);
        component.BreachTimer = _timing.CurTime + TimeSpan.FromSeconds(10);
        component.FinaleTimer = null;
        component.StigmaTimer = null;
        component.RiftTimer = null;

        AdjustCultObjectiveFinality(1);

        var sender = Loc.GetString("cosmiccult-announcement-sender");
        var mapData = _map.GetMap(_transform.GetMapId(component.StationGrid.ToCoordinates()));
        _chatSystem.DispatchStationAnnouncement(component.StationGrid, Loc.GetString("cosmiccult-announce-finale-progress"), sender, false, null, Color.FromHex("#4cabb3"));
        _chatSystem.DispatchStationAnnouncement(component.StationGrid, Loc.GetString("cosmiccult-announce-finale-warning"), null, false, null, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal(_finaleSound, Filter.Broadcast(), false, AudioParams.Default);

        EnsureComp<ParallaxComponent>(mapData, out var parallax);
        parallax.Parallax = "StellarParallaxMalignAlt2";
        Dirty(mapData, parallax);

        var shuntQuery = EntityQueryEnumerator<CosmicShuntedEntityComponent>();
        while (shuntQuery.MoveNext(out _, out var shuntComp))
        {
            shuntComp.ConvertOnReturn = false;
            shuntComp.ReadyToReturn = true;
        }

        var monumentQuery = EntityQueryEnumerator<CosmicMonumentComponent>();
        while (monumentQuery.MoveNext(out var monumentUid, out _))
        {
            _appearance.SetData(monumentUid, MonumentVisuals.Status, MonumentStatus.Finale);
        }

        HashSet<EntityUid> fontQueue = new();
        var fonts = EntityQueryEnumerator<CosmicFontComponent>();
        while (fonts.MoveNext(out var fontEnt, out var fontComp))
        {
            if (fontComp.Activated)
            {
                Spawn(fontComp.Plinth, Transform(fontEnt).Coordinates);
                Spawn(fontComp.GenericVfx, Transform(fontEnt).Coordinates);
                Spawn(_random.Pick(fontComp.Armors), Transform(fontEnt).Coordinates);
                Spawn(_random.Pick(fontComp.Weapons), Transform(fontEnt).Coordinates);
                fontQueue.Add(fontEnt);
            }
            else
                fontComp.FinaleRunning = true;
        }

        foreach (var font in fontQueue)
        {
            QueueDel(font);
        }

        var spawnPoints = EntityManager.GetAllComponents(typeof(CosmicVoidSpawnComponent)).ToImmutableList();
        foreach (var cultist in component.Cultists)
        {
            _cosmicCult.Brand(cultist);

            if (_container.IsEntityInContainer(cultist))
                _container.TryRemoveFromContainer(cultist);

            if (TryComp<CosmicShiftedComponent>(cultist, out var shiftComp))
            {
                _transform.Unanchor(cultist);
                _actions.RemoveAction(cultist, shiftComp.CosmicReturnActionActionEntity);
                RemComp<CosmicShiftedComponent>(cultist);
            }
            if (_mobState.IsAlive(cultist))
            {
                var destination = _transform.GetMapCoordinates(_random.Pick(spawnPoints).Uid);
                EnsureComp<BlockMovementComponent>(cultist);
                _cultShift.ShiftToDestination(cultist, destination);
            }
        }

        var resolvedMusic = _audio.ResolveSound(component.FinaleMusic);
        _sound.DispatchStationEventMusic(component.StationGrid, resolvedMusic, StationEventMusicType.CosmicCult);
    }

    private void FinaleSetup(EntityUid uid, CosmicCultRuleComponent component)
    {
        component.FinaleSetup = true;
        foreach (var cultist in component.Cultists)
        {
            if (TryComp<DoAfterComponent>(cultist, out var doAfterComp))
            {
                foreach (var doAfterId in doAfterComp.AwaitedDoAfters)
                {
                    _doAfter.Cancel(cultist, doAfterId.Key);
                }
            }

            if (TryComp<CosmicCultistComponent>(cultist, out var cultComp))
            {
                _actions.RemoveAction(cultist, cultComp.CosmicShiftActionActionEntity);
            }
        }
    }

    private void StartTier3(EntityUid uid, CosmicCultRuleComponent component)
    {
        component.Tier = 3;
        component.Tier3Timer = null;
        component.StigmaTimer = _timing.CurTime + _random.Next(TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(200));

        AdjustCultObjectiveFinality(1);

        var sender = Loc.GetString("cosmiccult-announcement-sender");
        var mapData = _map.GetMap(_transform.GetMapId(component.StationGrid.ToCoordinates()));
        _chatSystem.DispatchStationAnnouncement(component.StationGrid, Loc.GetString("cosmiccult-announce-tier3-progress"), sender, false, null, Color.FromHex("#4cabb3"));
        _chatSystem.DispatchStationAnnouncement(component.StationGrid, Loc.GetString("cosmiccult-announce-tier3-warning"), null, false, null, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal(_tier3Sound, Filter.Broadcast(), false, AudioParams.Default);

        EnsureComp<ParallaxComponent>(mapData, out var parallax);
        parallax.Parallax = "StellarParallaxMalignAlt";
        Dirty(mapData, parallax);

        for (var i = 0; i < Convert.ToInt16(component.TotalCrew / 3); i++) // spawn # stigma rifts equal to 33.37% of the playercount
        {
            SpawnStigma();
        }

        var query = EntityQueryEnumerator<CosmicCultistComponent>();
        while (query.MoveNext(out var cultistEnt, out var cultComp))
        {
            foreach (var influence in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influence => influence.Tier == 3))
            {
                if (cultComp.UnlockedInfluences.ContainsKey(influence))
                    continue;
                cultComp.UnlockedInfluences.Add(influence, influence.Weight);
            }
            IncrementCultistProgress((cultistEnt, cultComp), 12);
            Dirty(cultistEnt, cultComp);
        }
    }

    private void StartTier2(EntityUid uid, CosmicCultRuleComponent component)
    {
        component.Tier = 2;
        component.Tier2Timer = null;
        component.RiftTimer = _timing.CurTime + _random.Next(TimeSpan.FromSeconds(230), TimeSpan.FromSeconds(360));

        AdjustCultObjectiveFinality(1);

        var sender = Loc.GetString("cosmiccult-announcement-sender");
        _chatSystem.DispatchStationAnnouncement(component.StationGrid, Loc.GetString("cosmiccult-announce-tier2-progress"), sender, false, null, Color.FromHex("#4cabb3"));
        _chatSystem.DispatchStationAnnouncement(component.StationGrid, Loc.GetString("cosmiccult-announce-tier2-warning"), null, false, null, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal(_tier2Sound, Filter.Broadcast(), false, AudioParams.Default);

        for (var i = 0; i < Convert.ToInt16(component.TotalCrew / 6); i++) // spawn # malign rifts equal to 16.67% of the playercount
        {
            SpawnRift();
        }

        var query = EntityQueryEnumerator<CosmicCultistComponent>();
        while (query.MoveNext(out var cultistEnt, out var cultComp))
        {
            foreach (var influence in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influence => influence.Tier == 2))
            {
                if (cultComp.UnlockedInfluences.ContainsKey(influence))
                    continue;
                cultComp.UnlockedInfluences.Add(influence, influence.Weight);
            }

            IncrementCultistProgress((cultistEnt, cultComp), 8);
            Dirty(cultistEnt, cultComp);
        }
    }
    #endregion

    #region Round & Objectives

    private bool CultistsAlive(CosmicCultRuleComponent cult)
    {
        foreach (var cultist in cult.Cultists)
        {
            if (_mobState.IsAlive(cultist))
                return true;
        }
        return false;
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CosmicCultistComponent> ent, ref MobStateChangedEvent args)
    {
        if (AssociatedGamerule(ent.Owner) is not { } cult || args.NewMobState is MobState.Alive || CultistsAlive(cult))
            return;

        _sound.StopStationEventMusic(cult.Comp.StationGrid, StationEventMusicType.CosmicCult);
        _roundEnd.DoRoundEndBehavior(cult.Comp.RoundEndBehavior, cult.Comp.EvacShuttleTime, cult.Comp.RoundEndTextSender, cult.Comp.RoundEndTextShuttleCall, cult.Comp.RoundEndTextAnnouncement);
        cult.Comp.RoundEndBehavior = RoundEndBehavior.Nothing; // prevent this being called multiple times.
        cult.Comp.CultWin = false;
        cult.Comp.Tier2Timer = null;
        cult.Comp.Tier3Timer = null;
        cult.Comp.FinaleTimer = null;
        cult.Comp.CultWinTimer = null;
        cult.Comp.RiftTimer = null;
        cult.Comp.StigmaTimer = null;
        cult.Comp.BreachTimer = null;
    }

    [SubscribeLocalEvent]
    private void OnAssociatedShutdown(Entity<CosmicCultAssociatedRuleComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<CosmicCultRuleComponent>(ent.Comp.CultGamerule, out var cult))
            return;

        cult.Cultists.Remove(ent);
    }

    protected override void AppendRoundEndText(EntityUid uid, CosmicCultRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        // var ftlKey = component.WinType.ToString().ToLower(); // convert this to a ternary boolean operator. the ! = xyz : xhz thingo.
        //
        //
        // var winType = Loc.GetString($"cosmiccult-roundend-{ftlKey}");
        // var summaryText = Loc.GetString($"cosmiccult-summary-{ftlKey}");
        // args.AddLine(winType);
        // args.AddLine(summaryText);
        args.AddLine(Loc.GetString("cosmiccult-roundend-cultist-count", ("initialCount", component.TotalCult)));
        args.AddLine(Loc.GetString("cosmiccult-roundend-cultpop-count", ("count", Math.Round(component.PortionConverted * 100d))));
        args.AddLine(Loc.GetString("cosmiccult-roundend-entropy-count", ("count", component.EntropySiphoned)));
    }

    public void IncrementCultObjectiveEntropy(Entity<CosmicCultistComponent> ent)
    {
        if (AssociatedGamerule(ent) is not { } cult)
            return;

        cult.Comp.EntropySiphoned += ent.Comp.CosmicSiphonQuantity;
        UpdateCultData(cult);

        // var query = EntityQueryEnumerator<CosmicEntropyGoalComponent, StellarNumericGoalComponent>();
        // while (query.MoveNext(out var uid, out _, out var numeric))
        // {
        //     _numericGoal.SetCurrent((uid, numeric), cult.Comp.EntropySiphoned);
        // }
    }

    public void IncrementCultistProgress(Entity<CosmicCultistComponent> ent, int amount = 0)
    {
        var toIncrement = amount > 0 ? amount : 1;
        ent.Comp.PersonalProgress += toIncrement;

        if (ent.Comp.PersonalProgress >= 14 && _playerMan.TryGetSessionByEntity(ent, out var session))
        {
            ent.Comp.PersonalProgress -= 14;
            ent.Comp.MonumentVisits++;
            _euiMan.OpenEui(new CosmicInfluenceEui(), session);
            _audio.PlayEntity(ent.Comp.AbilityGainSfx, ent, ent);
        }
    }

    public void AdjustCultObjectiveConversion(int value)
    {
        // var query = EntityQueryEnumerator<CosmicConversionGoalComponent, StellarNumericGoalComponent>();
        // while (query.MoveNext(out var uid, out _, out var numeric))
        // {
        //     _numericGoal.ChangeCurrent((uid, numeric), value);
        // }
    }

    public void AdjustCultObjectiveFinality(int value)
    {
        // var query = EntityQueryEnumerator<CosmicFinalityGoalComponent, StellarNumericGoalComponent>();
        // while (query.MoveNext(out var uid, out _, out var numeric))
        // {
        //     _numericGoal.ChangeCurrent((uid, numeric), value);
        // }
    }
    #endregion

    #region De- & Conversion
    public void TryStartCult(EntityUid uid, Entity<CosmicCultRuleComponent> rule)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        EnsureComp<CosmicCultAssociatedRuleComponent>(uid, out var associatedComp);

        associatedComp.CultGamerule = rule;

        // _goals.ObserveContainer(mindId, rule.Comp.GoalsContainer!.Value);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels.Add("CosmicRadio");
        transmitter.Channels.Add("CosmicRadio");

        if (_playerMan.TryGetSessionById(mind.UserId, out var session))
            _euiMan.OpenEui(new CosmicRoundStartEui(), session);

        rule.Comp.TotalCult++;
        rule.Comp.Cultists.Add(uid);
    }

    [SubscribeLocalEvent]
    private void OnAssociateRule(ref CosmicCultAssociateRuleEvent args)
    {
        TransferCultAssociation(args.Originator, args.Target);
    }

    public void TransferCultAssociation(EntityUid from, EntityUid to)
    {
        if (!TryComp<CosmicCultAssociatedRuleComponent>(from, out var source))
            return;

        var destination = EnsureComp<CosmicCultAssociatedRuleComponent>(to);
        destination.CultGamerule = source.CultGamerule;
    }

    public Entity<CosmicCultRuleComponent>? AssociatedGamerule(EntityUid uid)
    {
        if (!TryComp<CosmicCultAssociatedRuleComponent>(uid, out var associated))
        {
            _sawmill.Debug("{0} has no associated rule", uid);
            return null;
        }

        if (!TryComp<CosmicCultRuleComponent>(associated.CultGamerule, out var cult))
        {
            _sawmill.Debug("Associated gamerule {0} is not a cult gamerule", associated.CultGamerule);
            return null;
        }

        return (associated.CultGamerule, cult);
    }

    public void CosmicConversion(EntityUid converter, EntityUid uid)
    {
        if (AssociatedGamerule(converter) is not { } cult)
            return;

        if (!_mind.TryGetMind(uid, out var mindId, out var mind) || !_playerMan.TryGetSessionById(mind.UserId, out var session))
            return;

        _role.MindAddRole(mindId, MindRole, mind, true);
        _antag.SendBriefing(session, Loc.GetString("cosmiccult-conversion-greeting"), Color.FromHex("#4cabb3"), _briefingSound);

        var cultComp = EnsureComp<CosmicCultistComponent>(uid);
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        TransferCultAssociation(converter, uid);

        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels = ["CosmicRadio"];
        transmitter.Channels = ["CosmicRadio"];

        // _goals.ObserveContainer(mindId, cult.Comp.GoalsContainer!.Value);
        _euiMan.OpenEui(new CosmicConvertedEui(), session);
        cult.Comp.TotalCult++;
        cult.Comp.Cultists.Add(uid);

        if (cult.Comp.Tier >= 2)
        {
            foreach (var influence in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influence => influence.Tier <= cult.Comp.Tier && influence.Tier != 1))
            {
                if (cultComp.UnlockedInfluences.ContainsKey(influence))
                    continue;
                cultComp.UnlockedInfluences.Add(influence, influence.Weight);
            }
        }

        if (cult.Comp.CultWinTimer is { } winTimer)
            _cosmicCult.Brand(uid);

        cultComp.MonumentVisits = cult.Comp.Tier;

        AdjustCultObjectiveConversion(1);
        UpdateCultData(cult);
        Dirty(uid, cultComp);
    }
    #endregion
}
