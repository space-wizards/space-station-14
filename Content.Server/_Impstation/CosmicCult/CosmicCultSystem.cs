using System.Net.Mime;
using Content.Server.Popups;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Server.Actions;
using Content.Server.GameTicking.Events;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Robust.Shared.Timing;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Interaction;
using Content.Server.Audio;
using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage;
using Content.Server.AlertLevel;
using Content.Shared.SSDIndicator;
using Content.Server.Announcements.Systems;
using Content.Server.Pinpointer;
using Robust.Shared.Utility;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Server.Ghost;
using Content.Server.Polymorph.Systems;
using Robust.Shared.Map;
using Content.Server.Station.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Content.Server.Doors.Systems;
using Content.Server.Light.EntitySystems;
using Content.Server.Flash;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared.Camera;
using Robust.Shared.Player;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly CosmicGlyphSystem _cosmicGlyphs = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly AlertLevelSystem _alert = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    private const string MapPath = "Maps/_Impstation/Nonstations/cosmicvoid.yml";
    public int CultistCount;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnStartCultist);
        SubscribeLocalEvent<CosmicCultLeadComponent, ComponentInit>(OnStartCultLead);
        SubscribeLocalEvent<MonumentComponent, ComponentInit>(OnStartMonument);
        SubscribeLocalEvent<MonumentComponent, InteractUsingEvent>(OnInfuseEntropy);

        SubscribeLocalEvent<InfluenceStrideComponent, ComponentInit>(OnStartInfluenceStride);
        SubscribeLocalEvent<InfluenceStrideComponent, ComponentRemove>(OnEndInfluenceStride);
        SubscribeLocalEvent<InfluenceStrideComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<CosmicImposingComponent, ComponentInit>(OnStartImposition);
        SubscribeLocalEvent<CosmicImposingComponent, ComponentRemove>(OnEndImposition);
        SubscribeLocalEvent<CosmicImposingComponent, RefreshMovementSpeedModifiersEvent>(OnImpositionMoveSpeed);

        MakeSimpleExamineHandler<CosmicMarkStructureComponent>("cosmic-examine-text-structures");
        MakeSimpleExamineHandler<CosmicMarkBlankComponent>("cosmic-examine-text-abilityblank");
        MakeSimpleExamineHandler<CosmicMarkLapseComponent>("cosmic-examine-text-abilitylapse");
        MakeSimpleExamineHandler<CosmicMarkEchoComponent>("cosmic-examine-text-malignecho");
        MakeSimpleExamineHandler<CosmicImposingComponent>("cosmic-examine-text-imposition");
        MakeSimpleExamineHandler<CosmicMarkGodComponent>("cosmic-examine-text-god");

        SubscribeAbilities(); //Hook up the cosmic cult ability system
        SubscribeFinale(); //Hook up the cosmic cult finale system
    }
    #region Housekeeping

    /// <summary>
    /// Creates the Cosmic Void pocket dimension map.
    /// </summary>
    private void OnRoundStart(RoundStartingEvent ev)
    {
        _map.CreateMap(out var mapId);
        var options = new MapLoadOptions { LoadMap = true };
        if (_mapLoader.TryLoad(mapId, MapPath, out _, options))
            _map.SetPaused(mapId, false);
    }

    public override void Update(float frameTime) // This Update() can fit so much functionality in it
    {
        base.Update(frameTime);

        var shuntQuery = EntityQueryEnumerator<InVoidComponent>(); // Enumerator for Shunt Subjectivity's cosmic void pocket dimension
        while (shuntQuery.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ExitVoidTime)
            {
                if (!TryComp<MindContainerComponent>(uid, out var mindContainer))
                    continue;
                var mindEnt = mindContainer.Mind!.Value;
                var mind = Comp<MindComponent>(mindEnt);
                mind.PreventGhosting = false;
                _mind.TransferTo(mindEnt, comp.OriginalBody);
                RemComp<CosmicMarkBlankComponent>(comp.OriginalBody);
                _popup.PopupEntity(Loc.GetString("cosmicability-blank-return"), comp.OriginalBody, comp.OriginalBody);
                QueueDel(uid);
            }
        }
        var imposeQuery = EntityQueryEnumerator<CosmicImposingComponent>(); // Enumerator for removing Vacuous Imposition's active effects & removing atmosphere.
        while (imposeQuery.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ImposeCheckTimer)
            {
                RemCompDeferred<CosmicImposingComponent>(uid);
            }
        }
        var vitQuery = EntityQueryEnumerator<MonumentComponent>(); // Enumerator for people who've unlocked Vacuous Vitality.
        while (vitQuery.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.VitalityCheckTimer)
            {
                var entities = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 10);
                entities.RemoveWhere(entity => !HasComp<InfluenceVitalityComponent>(entity));
                comp.VitalityCheckTimer = _timing.CurTime + comp.CheckWait;
                foreach (var entity in entities) _damageable.TryChangeDamage(entity, comp.MonumentHealing * -1);
            }
        }
        var finaleQuery = EntityQueryEnumerator<CosmicFinaleComponent>(); // Enumerator for The Monument's Finale. All of it.
        while (finaleQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.FinaleActive && !comp.BufferComplete && !comp.PlayedBufferSong && !string.IsNullOrEmpty(comp.SelectedBufferSong))
            {
                _sound.DispatchStationEventMusic(uid, comp.SelectedBufferSong, StationEventMusicType.CosmicCult);
                _announcer.SendAnnouncementMessage(_announcer.GetAnnouncementId("SpawnAnnounceCaptain"),
                Loc.GetString("cosmiccult-finale-location", ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, Transform(uid)))))),
                null,
                Color.FromHex("#cae8e8"));
                comp.PlayedBufferSong = true;
            }
            else if (comp.FinaleActive && comp.FinaleTimer <= comp.FinaleSongLength && !comp.PlayedFinaleSong && !string.IsNullOrEmpty(comp.SelectedFinaleSong) && comp.BufferComplete && !comp.PlayedFinaleSong)
            {
                _sound.DispatchStationEventMusic(uid, comp.SelectedFinaleSong, StationEventMusicType.CosmicCult);
                _announcer.SendAnnouncementMessage(_announcer.GetAnnouncementId("SpawnAnnounceCaptain"),
                Loc.GetString("cosmiccult-finale-location", ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, Transform(uid)))))),
                null,
                Color.FromHex("#cae8e8"));
                comp.PlayedFinaleSong = true;
            }
            if (comp.FinaleActive && _timing.CurTime >= comp.BufferTimer && comp.FinaleActive && !comp.BufferComplete && !comp.Victory)
            {
                _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);
                comp.FinaleTimer = _timing.CurTime + comp.FinaleRemainingTime;
                comp.SelectedFinaleSong = _audio.GetSound(comp.FinaleMusic);
                comp.FinaleSongLength = TimeSpan.FromSeconds(_audio.GetAudioLength(comp.SelectedFinaleSong).TotalSeconds);
                _sound.DispatchStationEventMusic(uid, comp.SelectedFinaleSong, StationEventMusicType.CosmicCult);
                comp.BufferComplete = true;
                comp.PlayedFinaleSong = true;
                _appearance.SetData(uid, MonumentVisuals.FinaleReached, 3);
            }
            else if (comp.FinaleActive && _timing.CurTime >= comp.FinaleTimer && comp.FinaleActive && comp.BufferComplete && !comp.Victory)
            {
                _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);
                Spawn("MobCosmicGodSpawn", Transform(uid).Coordinates);
                comp.Victory = true;
            }
            if (_timing.CurTime >= comp.CultistsCheckTimer && comp.FinaleActive && !comp.BufferComplete)
            {
                comp.CultistsCheckTimer = _timing.CurTime + comp.CheckWait;
                var cultistsPresent = CultistCount = _cosmicGlyphs.GatherCultists(uid, 5).Count; //Let's use the cultist collecting hashset from Cosmic Glyphs to see how many folks are around!
                CultistCount = int.Clamp(cultistsPresent, 0, 10);
                _popup.PopupCoordinates(Loc.GetString("cosmiccult-finale-cultist-count", ("COUNT", CultistCount)), Transform(uid).Coordinates);
                var modifyTime = TimeSpan.FromSeconds(360 * 5 / (360 - 25 * CultistCount) - 5);
                comp.BufferTimer -= modifyTime;
            }
        }
    }

    /// <summary>
    /// Parses marker components to output their respective loc strings directly into your examine box, courtesy of TGRCdev(Github).
    /// </summary>
    private void MakeSimpleExamineHandler<TComp>(LocId message)
    where TComp: IComponent
    {
        SubscribeLocalEvent((Entity<TComp> ent, ref ExaminedEvent args) => {
            if (HasComp<CosmicCultComponent>(args.Examiner))
                args.PushMarkup(Loc.GetString("cosmic-examine-text-forthecult"));
            else
                args.PushMarkup(Loc.GetString(message, ("entity", ent.Owner)));
        });
    }
    #endregion

    #region Init Cult
    /// <summary>
    /// Add the starting powers to the cultist.
    /// </summary>
    private void OnStartCultist(Entity<CosmicCultComponent> uid, ref ComponentInit args)
    {
        foreach (var actionId in uid.Comp.CosmicCultActions)
        {
            var actionEnt = _actions.AddAction(uid, actionId);
            uid.Comp.ActionEntities.Add(actionEnt);
        }
        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | MonumentComponent.LayerMask);
    }

    /// <summary>
    /// Add the Monument summon action to the cult lead.
    /// </summary>
    private void OnStartCultLead(Entity<CosmicCultLeadComponent> uid, ref ComponentInit args)
    {
        _actions.AddAction(uid, ref uid.Comp.CosmicMonumentActionEntity, uid.Comp.CosmicMonumentAction, uid);
    }

    /// <summary>
    /// Called by Cosmic Siphon. Increments the Cult's global objective tracker.
    /// </summary>
    #endregion

    #region Entropy
    private void OnStartMonument(Entity<MonumentComponent> uid, ref ComponentInit args)
    {
        _cultRule.MonumentTier1(uid);
        _cultRule.UpdateCultData(uid);
    }

    private void OnInfuseEntropy(Entity<MonumentComponent> uid, ref InteractUsingEvent args)
    {
        if (!HasComp<CosmicEntropyMoteComponent>(args.Used) || !HasComp<CosmicCultComponent>(args.User) || !uid.Comp.Enabled || args.Handled)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-entropy-unavailable"), args.User, args.User);
            return;
        }

        args.Handled = AddEntropy(uid, args.Used, args.User);
    }
    private bool AddEntropy(Entity<MonumentComponent> monument, EntityUid entropy, EntityUid cultist)
    {
        var quant = TryComp<StackComponent>(entropy, out var stackComp) ? stackComp.Count : 1;
        if (TryComp<CosmicCultComponent>(cultist, out var cultComp))
        {
            cultComp.EntropyBudget += quant;
            Dirty(cultist, cultComp);
        }

        monument.Comp.TotalEntropy += quant;
        _cultRule.UpdateCultData(monument);

        _popup.PopupEntity(Loc.GetString("cosmiccult-entropy-inserted", ("count", quant)), cultist, cultist);
        _audio.PlayEntity("/Audio/_Impstation/CosmicCult/insert_entropy.ogg", cultist, monument);
        QueueDel(entropy);
        return true;
    }
    #endregion

    #region Movespeed
    private void OnStartInfluenceStride(Entity<InfluenceStrideComponent> uid, ref ComponentInit args) // i wish movespeed was easier to work with
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnStartImposition(Entity<CosmicImposingComponent> uid, ref ComponentInit args) // these functions just make sure
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnEndInfluenceStride(Entity<InfluenceStrideComponent> uid, ref ComponentRemove args) // that movespeed applies more-or-less correctly
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnEndImposition(Entity<CosmicImposingComponent> uid, ref ComponentRemove args) // as various cosmic cult effects get added and removed
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, InfluenceStrideComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (HasComp<InfluenceStrideComponent>(uid))
            args.ModifySpeed(1.05f, 1.05f);
        else
            args.ModifySpeed(1f, 1f);
    }

    private void OnImpositionMoveSpeed(EntityUid uid, CosmicImposingComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (HasComp<CosmicImposingComponent>(uid))
            args.ModifySpeed(0.55f, 0.55f);
        else
            args.ModifySpeed(1f, 1f);
    }
    #endregion

}
