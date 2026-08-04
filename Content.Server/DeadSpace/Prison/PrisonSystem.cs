using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.DeadSpace.Prison.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Prison;

public sealed class PrisonSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;

    private readonly HashSet<NetUserId> _prisonUsers = [];
    private readonly Dictionary<EntityUid, Dictionary<EntityUid, FixedPoint2>> _prisonDamageByTarget = new();
    private readonly Dictionary<EntityUid, Dictionary<EntityUid, FixedPoint2>> _prisonFaunaDamageByTarget = new();
    private readonly Dictionary<NetUserId, PendingFaunaReward> _pendingFaunaRewards = new();
    private readonly HashSet<NetUserId> _faunaRewardInProgress = [];
    private readonly object _faunaRewardLock = new();
    private static readonly ProtoId<StartingGearPrototype> PrisonerGear = "PrisonerGear";
    private const int SourceParentSearchDepth = 6;
    private bool _enabled;
    private int _murderPenaltyMinutes;

    private readonly TimeSpan _safeguardUpdateRate = TimeSpan.FromSeconds(10);
    private TimeSpan _nextSafeguardUpdate;

    private readonly TimeSpan _activeBanRefreshRate = TimeSpan.FromMinutes(1);
    private TimeSpan _nextActiveBanRefresh;
    private bool _activeBanRefreshRunning;

    public bool Enabled => _enabled;
    public bool Ready => _enabled && TryGetSpawnCoordinates(out _);

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCCCVars.PrisonEnabled, value => _enabled = value, true);
        Subs.CVar(_cfg, CCCCVars.PrisonMurderPenaltyMinutes, value => _murderPenaltyMinutes = value, true);

        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnPlayerBeforeSpawn);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MindRoleAddAttemptEvent>(OnMindRoleAddAttempt);
        SubscribeLocalEvent<AttackAttemptEvent>(OnPrisonerAttackAttempt);
        SubscribeLocalEvent<AttemptShootEvent>(OnPrisonerAttemptShoot);
        SubscribeLocalEvent<DamageableComponent, DamageModifyEvent>(OnPrisonerDamageModify);
        SubscribeLocalEvent<PrisonBoundComponent, DamageChangedEvent>(OnPrisonDamageChanged, before: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<PrisonSpawnedFaunaComponent, DamageChangedEvent>(OnPrisonFaunaDamageChanged, before: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<MobStateChangedEvent>(OnPrisonMobStateChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _prisonDamageByTarget.Clear();
        _prisonFaunaDamageByTarget.Clear();
        lock (_faunaRewardLock)
        {
            _pendingFaunaRewards.Clear();
            _faunaRewardInProgress.Clear();
        }
    }

    private void OnPrisonerAttackAttempt(AttackAttemptEvent args)
    {
        if (args.Cancelled || !IsEntityPrisoner(args.Uid))
        {
            return;
        }

        if (!TryComp(args.Uid, out TransformComponent? xform) || !IsPrisonMap(xform.MapID))
        {
            args.Cancel();
            return;
        }

        if (args.Target is { } target &&
            TryGetMind(target, out var targetMindId, out var targetMind) &&
            !IsMindPrisoner(targetMindId, targetMind))
        {
            args.Cancel();
        }
    }

    private void OnPrisonerAttemptShoot(ref AttemptShootEvent args)
    {
        if (!args.Cancelled && IsEntityPrisoner(args.User) &&
            (!TryComp(args.User, out TransformComponent? xform) || !IsPrisonMap(xform.MapID)))
        {
            args.Cancelled = true;
        }
    }

    private void OnPrisonerDamageModify(EntityUid target, DamageableComponent component, DamageModifyEvent args)
    {
        if (args.Damage.Empty ||
            !TryGetDamageSourceMind(args.Origin, out var sourceMindId, out var sourceMind) ||
            !IsMindPrisoner(sourceMindId, sourceMind) ||
            !TryGetMind(target, out var targetMindId, out var targetMind) ||
            IsMindPrisoner(targetMindId, targetMind))
        {
            return;
        }

        args.Damage = new DamageSpecifier();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime >= _nextSafeguardUpdate)
        {
            _nextSafeguardUpdate = _timing.CurTime + _safeguardUpdateRate;
            SafeguardPrisoners();
        }

        if (_prisonUsers.Count == 0 ||
            _activeBanRefreshRunning ||
            _timing.CurTime < _nextActiveBanRefresh)
        {
            return;
        }

        _nextActiveBanRefresh = _timing.CurTime + _activeBanRefreshRate;
        RefreshActivePrisonBans();
    }

    public bool RegisterPrisonerConnection(NetUserId userId, IReadOnlyCollection<BanDef> bans)
    {
        if (IsUserCurrentlyAntagonist(userId))
            return false;

        if (!CanUsePrisonForBans(bans))
            return false;

        _prisonUsers.Add(userId);
        return true;
    }

    public bool CanUsePrisonForBans(IReadOnlyCollection<BanDef> bans)
    {
        if (!_enabled || !Ready || bans.Count == 0)
            return false;

        return GetLatestActiveServerBan(bans)?.SendToPrison == true;
    }

    public bool TrySendToPrison(ICommonSession session, BanDef ban)
    {
        if (IsSessionAntagonist(session))
            return false;

        if (!_enabled || !Ready || !IsPrisonServerBan(ban))
            return false;

        _prisonUsers.Add(session.UserId);
        var registered = new PrisonerRegisteredEvent(session);
        RaiseLocalEvent(ref registered);

        // A lobby session has no round body to move. Keep the connection alive and
        // let PlayerBeforeSpawn create the prisoner body when they actually join.
        if (!_gameTicker.UserHasJoinedGame(session))
        {
            SendPrisonMessage(session, ban);
            return true;
        }

        if (!TryGetSpawnCoordinates(out var coordinates))
            return false;

        if (session.AttachedEntity is { } entity && Exists(entity) && !HasComp<GhostComponent>(entity))
        {
            SendEntityToPrison(entity, coordinates);
        }
        else
        {
            if (!TryGetHumanoidProfile(session, out var profile))
                return false;

            SpawnPrisonMob(session, profile, coordinates);
        }
        SendPrisonMessage(session, ban);
        return true;
    }

    public bool IsUserPrisoner(NetUserId userId)
    {
        if (_prisonUsers.Contains(userId))
            return true;

        return _player.TryGetSessionById(userId, out var session)
               && session.AttachedEntity is { } entity
               && HasComp<PrisonBoundComponent>(entity);
    }

    public async Task<PrisonSentence?> GetReducibleSentence(NetUserId userId)
    {
        if (!IsUserPrisoner(userId) || !_player.TryGetSessionById(userId, out var session))
            return null;

        var check = CreateBanRefreshCheck(session);
        var bans = await _db.GetBansAsync(
            check.Address,
            check.UserId,
            check.HwId,
            check.ModernHwIds,
            includeUnbanned: false);
        var latest = GetLatestActiveServerBan(bans);

        if (latest?.Id is not { } banId ||
            !IsPrisonServerBan(latest) ||
            latest.ExpirationTime == null)
        {
            return null;
        }

        return new PrisonSentence(banId);
    }

    public async Task<TimeSpan> TryReduceSentence(
        NetUserId userId,
        int expectedBanId,
        TimeSpan reduction)
    {
        if (reduction <= TimeSpan.Zero)
            return TimeSpan.Zero;

        // Reload before every atomic update so simultaneous fauna and ore rewards cannot overwrite each other.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var bans = await _db.GetBansAsync(
                null,
                userId,
                null,
                null,
                includeUnbanned: false);
            var latest = GetLatestActiveServerBan(bans);
            if (latest?.Id != expectedBanId ||
                !IsPrisonServerBan(latest) ||
                latest.ExpirationTime is not { } expiration)
            {
                return TimeSpan.Zero;
            }

            var now = DateTimeOffset.UtcNow;
            var updated = expiration - reduction;
            if (updated < now)
                updated = now;

            if (!await _db.TrySetActivePrisonBanExpiration(expectedBanId, expiration, updated))
                continue;

            return expiration - updated;
        }

        return TimeSpan.Zero;
    }

    public void RefreshPrisonBanState()
    {
        _nextActiveBanRefresh = TimeSpan.Zero;

        if (_activeBanRefreshRunning)
            return;

        RefreshActivePrisonBans();
    }

    public bool IsEntityPrisoner(EntityUid entity)
    {
        if (HasComp<PrisonBoundComponent>(entity))
            return true;

        return _mind.TryGetMind(entity, out var mindId, out var mind)
               && IsMindPrisoner(mindId, mind);
    }

    public bool IsMindPrisoner(EntityUid mindId, MindComponent? mind = null)
    {
        return Resolve(mindId, ref mind, false)
               && mind.UserId is { } userId
               && IsUserPrisoner(userId);
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        if (!IsUserPrisoner(ev.PlayerSession.UserId))
            return;

        _chat.DispatchServerMessage(ev.PlayerSession, Loc.GetString("prison-chat-join-message"));
    }

    private void OnPlayerBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        if (!IsUserPrisoner(ev.Player.UserId))
            return;

        ev.Handled = true;

        if (!_enabled || !TryGetSpawnCoordinates(out var coordinates))
        {
            ev.Player.Channel.Disconnect(Loc.GetString("prison-unavailable-message"));
            return;
        }

        SpawnPrisonMob(ev.Player, ev.Profile, coordinates);
        _chat.DispatchServerMessage(ev.Player, Loc.GetString("prison-arrival-message"));
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!IsUserPrisoner(ev.Player.UserId) && !HasComp<PrisonBoundComponent>(ev.Entity))
            return;

        _prisonUsers.Add(ev.Player.UserId);

        if (!_enabled || !TryGetSpawnCoordinates(out var coordinates))
        {
            ev.Player.Channel.Disconnect(Loc.GetString("prison-unavailable-message"));
            return;
        }

        if (HasComp<GhostComponent>(ev.Entity))
        {
            RemComp<PrisonBoundComponent>(ev.Entity);
            return;
        }

        var xform = Transform(ev.Entity);
        if (IsPrisonMap(xform.MapID))
            return;

        SendEntityToPrison(ev.Entity, coordinates);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _prisonUsers.Remove(e.Session.UserId);
    }

    private void OnMindRoleAddAttempt(MindRoleAddAttemptEvent args)
    {
        if (!args.Antagonist || args.Mind.UserId is not { } userId || !IsUserPrisoner(userId))
            return;

        args.Cancel();

        if (_player.TryGetSessionById(userId, out var session))
            _chat.DispatchServerMessage(session, Loc.GetString("prison-antag-role-blocked"));
    }

    private void OnPrisonDamageChanged(EntityUid uid, PrisonBoundComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
                _prisonDamageByTarget.Remove(uid);

            return;
        }

        if (!TryGetPrisonerMind(uid, out var targetMindId, out _))
        {
            _prisonDamageByTarget.Remove(uid);
            return;
        }

        var delta = args.DamageDelta.GetTotal();
        if (!args.DamageIncreased)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
            {
                _prisonDamageByTarget.Remove(uid);
                return;
            }

            ReducePrisonDamageContributors(uid, -delta);
            return;
        }

        if (delta <= FixedPoint2.Zero ||
            !TryGetDamageSourceMind(args.Origin, out var sourceMindId, out var sourceMind) ||
            sourceMindId == targetMindId ||
            !IsMindPrisoner(sourceMindId, sourceMind))
        {
            return;
        }

        if (!_prisonDamageByTarget.TryGetValue(uid, out var sourceDamage))
        {
            sourceDamage = new Dictionary<EntityUid, FixedPoint2>();
            _prisonDamageByTarget[uid] = sourceDamage;
        }

        sourceDamage[sourceMindId] = sourceDamage.GetValueOrDefault(sourceMindId) + delta;
    }

    private void OnPrisonFaunaDamageChanged(
        EntityUid uid,
        PrisonSpawnedFaunaComponent component,
        DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
                _prisonFaunaDamageByTarget.Remove(uid);

            return;
        }

        var delta = args.DamageDelta.GetTotal();
        if (!args.DamageIncreased)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
                _prisonFaunaDamageByTarget.Remove(uid);
            else
                ReduceDamageContributors(_prisonFaunaDamageByTarget, uid, -delta);

            return;
        }

        if (delta <= FixedPoint2.Zero ||
            !TryGetDamageSourceMind(args.Origin, out var sourceMindId, out var sourceMind) ||
            !IsMindPrisoner(sourceMindId, sourceMind))
        {
            return;
        }

        if (!_prisonFaunaDamageByTarget.TryGetValue(uid, out var sourceDamage))
        {
            sourceDamage = new Dictionary<EntityUid, FixedPoint2>();
            _prisonFaunaDamageByTarget[uid] = sourceDamage;
        }

        sourceDamage[sourceMindId] = sourceDamage.GetValueOrDefault(sourceMindId) + delta;
    }

    private void OnPrisonMobStateChanged(MobStateChangedEvent args)
    {
        if (!_enabled ||
            args.NewMobState != MobState.Dead ||
            args.OldMobState >= args.NewMobState)
        {
            return;
        }

        if (TryComp<PrisonSpawnedFaunaComponent>(args.Target, out var fauna))
        {
            OnPrisonFaunaKilled((args.Target, fauna), ref args);
            return;
        }

        if (_murderPenaltyMinutes <= 0)
            return;

        var target = args.Target;
        if (!TryGetPrisonerMind(target, out var targetMindId, out _))
        {
            _prisonDamageByTarget.Remove(target);
            return;
        }

        if (TryGetDamageSourceMind(args.Origin, out var sourceMindId, out var sourceMind) &&
            sourceMindId != targetMindId &&
            IsMindPrisoner(sourceMindId, sourceMind))
        {
            AddPrisonMurderPenalty(sourceMind);
            _prisonDamageByTarget.Remove(target);
            return;
        }

        if (TryGetLargestPrisonDamageContributor(target, targetMindId, out _, out var contributorMind))
            AddPrisonMurderPenalty(contributorMind);

        _prisonDamageByTarget.Remove(target);
    }

    private void OnPrisonFaunaKilled(Entity<PrisonSpawnedFaunaComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.SentenceReductionMinutes <= 0)
            return;

        MindComponent? sourceMind = null;
        if (TryGetDamageSourceMind(args.Origin, out var sourceMindId, out sourceMind))
        {
            if (!IsMindPrisoner(sourceMindId, sourceMind))
            {
                _prisonFaunaDamageByTarget.Remove(ent.Owner);
                return;
            }
        }
        else
        {
            if (!TryGetLargestPrisonFaunaDamageContributor(ent.Owner, out sourceMindId, out sourceMind))
            {
                _prisonFaunaDamageByTarget.Remove(ent.Owner);
                return;
            }
        }

        _prisonFaunaDamageByTarget.Remove(ent.Owner);
        if (sourceMind.UserId is not { } userId || !_player.TryGetSessionById(userId, out var session))
            return;

        var check = CreateBanRefreshCheck(session);
        var startRewardTask = false;
        lock (_faunaRewardLock)
        {
            var pendingMinutes = _pendingFaunaRewards.TryGetValue(userId, out var pending)
                ? pending.Minutes
                : 0;
            _pendingFaunaRewards[userId] = new PendingFaunaReward(
                check,
                pendingMinutes + ent.Comp.SentenceReductionMinutes);
            startRewardTask = _faunaRewardInProgress.Add(userId);
        }

        if (startRewardTask)
            ApplyPendingFaunaRewards(userId);
    }

    private async void ApplyPendingFaunaRewards(NetUserId userId)
    {
        while (true)
        {
            PendingFaunaReward pending;
            lock (_faunaRewardLock)
            {
                if (!_pendingFaunaRewards.Remove(userId, out pending))
                {
                    _faunaRewardInProgress.Remove(userId);
                    return;
                }
            }

            if (pending.Minutes <= 0)
                continue;

            try
            {
                var check = pending.Check;
                var bans = await _db.GetBansAsync(
                    check.Address,
                    check.UserId,
                    check.HwId,
                    check.ModernHwIds,
                    includeUnbanned: false);
                var latestBan = GetLatestActiveServerBan(bans);

                if (latestBan?.Id is not { } banId ||
                    !IsPrisonServerBan(latestBan) ||
                    latestBan.ExpirationTime is not { } expiration)
                {
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                var updatedExpiration = expiration - TimeSpan.FromMinutes(pending.Minutes);
                if (updatedExpiration < now)
                    updatedExpiration = now;

                if (!await _db.TrySetActivePrisonBanExpiration(banId, expiration, updatedExpiration))
                    continue;

                var appliedMinutes = Math.Min(
                    pending.Minutes,
                    Math.Max(0, (int) Math.Ceiling((expiration - now).TotalMinutes)));

                _taskManager.RunOnMainThread(() =>
                {
                    _nextActiveBanRefresh = TimeSpan.Zero;
                    if (_player.TryGetSessionById(userId, out var currentSession))
                    {
                        _chat.DispatchServerMessage(
                            currentSession,
                            Loc.GetString("prison-fauna-reward-message", ("minutes", appliedMinutes)));
                    }

                    RefreshPrisonBanState();
                });
            }
            catch (Exception e)
            {
                Log.Error($"Failed to apply prison fauna reward for {userId}: {e}");
            }
        }
    }

    private void SpawnPrisonMob(ICommonSession session, HumanoidCharacterProfile profile, EntityCoordinates coordinates)
    {
        if (_mind.TryGetMind(session.UserId, out _, out var existingMind) && !existingMind.IsVisitingEntity)
            _mind.WipeMind(session);

        var newMind = _mind.CreateMind(session.UserId, profile.Name);
        _mind.SetUserId(newMind, session.UserId);

        var mob = _spawning.SpawnPlayerMob(coordinates, null, profile, null);
        _mind.TransferTo(newMind, mob);

        EnsureComp<PrisonBoundComponent>(mob);
        EquipPrisoner(mob);
        _prisonUsers.Add(session.UserId);
    }

    private bool TryGetHumanoidProfile(ICommonSession session, [NotNullWhen(true)] out HumanoidCharacterProfile? profile)
    {
        if (_preferences.TryGetCachedPreferences(session.UserId, out var preferences) &&
            preferences.SelectedCharacter is HumanoidCharacterProfile humanoid)
        {
            profile = humanoid;
            return true;
        }

        profile = null;
        return false;
    }

    private void SendEntityToPrison(EntityUid entity, EntityCoordinates coordinates)
    {
        DropInventory(entity);

        _transform.SetCoordinates(entity, coordinates);
        _transform.AttachToGridOrMap(entity);

        EnsureComp<PrisonBoundComponent>(entity);
        EquipPrisoner(entity);
    }

    private void EquipPrisoner(EntityUid entity)
    {
        _spawning.EquipStartingGear(entity, PrisonerGear, raiseEvent: false);
    }

    private void DropInventory(EntityUid entity)
    {
        if (_inventory.TryGetContainerSlotEnumerator(entity, out var enumerator))
        {
            while (enumerator.NextItem(out var item, out var slot))
            {
                if (_inventory.TryUnequip(entity, entity, slot.Name, true, true))
                    _physics.ApplyAngularImpulse(item, ThrowingSystem.ThrowAngularImpulse);
            }
        }

        if (!TryComp(entity, out HandsComponent? hands))
            return;

        foreach (var hand in _hands.EnumerateHands((entity, hands)))
        {
            _hands.TryDrop((entity, hands), hand, checkActionBlocker: false, doDropInteraction: false);
        }
    }

    private void SafeguardPrisoners()
    {
        if (!_enabled || !TryGetSpawnCoordinates(out var coordinates))
            return;

        var query = EntityQueryEnumerator<PrisonBoundComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<GhostComponent>(uid))
            {
                RemCompDeferred<PrisonBoundComponent>(uid);
                continue;
            }

            if (IsPrisonMap(xform.MapID))
                continue;

            SendEntityToPrison(uid, coordinates);
        }
    }

    private async void RefreshActivePrisonBans()
    {
        _activeBanRefreshRunning = true;

        try
        {
            var checks = new List<PrisonBanRefreshCheck>();

            foreach (var userId in _prisonUsers.ToArray())
            {
                if (!_player.TryGetSessionById(userId, out var session))
                {
                    _prisonUsers.Remove(userId);
                    continue;
                }

                checks.Add(CreateBanRefreshCheck(session));
            }

            if (checks.Count == 0)
                return;

            var results = new List<PrisonBanRefreshResult>();
            foreach (var check in checks)
            {
                var bans = await _db.GetBansAsync(
                    check.Address,
                    check.UserId,
                    check.HwId,
                    check.ModernHwIds,
                    includeUnbanned: false);

                results.Add(new PrisonBanRefreshResult(
                    check.UserId,
                    GetLatestActiveServerBan(bans)));
            }

            _taskManager.RunOnMainThread(() => ApplyActivePrisonBanRefresh(results));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to refresh prison ban state: {e}");
        }
        finally
        {
            _activeBanRefreshRunning = false;
        }
    }

    private PrisonBanRefreshCheck CreateBanRefreshCheck(ICommonSession session)
    {
        var channel = session.Channel;
        ImmutableArray<byte>? hwId = channel.UserData.HWId;

        if (hwId.Value.Length == 0 || !_cfg.GetCVar(CCVars.BanHardwareIds))
            hwId = null;

        return new PrisonBanRefreshCheck(
            session.UserId,
            channel.RemoteEndPoint.Address,
            hwId,
            channel.UserData.ModernHWIds);
    }

    private void ApplyActivePrisonBanRefresh(List<PrisonBanRefreshResult> results)
    {
        foreach (var result in results)
        {
            if (!_player.TryGetSessionById(result.UserId, out var session))
            {
                _prisonUsers.Remove(result.UserId);
                continue;
            }

            if (result.LatestBan is { SendToPrison: false } directBan)
            {
                ClearPrisonState(session);
                session.Channel.Disconnect(directBan.FormatBanMessage(_cfg, _loc));
                continue;
            }

            if (result.LatestBan == null)
            {
                ClearPrisonState(session);
                _chat.DispatchServerMessage(session, Loc.GetString("prison-release-message"));
                continue;
            }

            if (!Ready)
            {
                ClearPrisonState(session);
                session.Channel.Disconnect(result.LatestBan.FormatBanMessage(_cfg, _loc));
            }
        }
    }

    private void ClearPrisonState(ICommonSession session)
    {
        _prisonUsers.Remove(session.UserId);

        if (session.AttachedEntity is { } entity && Exists(entity))
            RemComp<PrisonBoundComponent>(entity);
    }

    private bool TryGetPrisonerMind(EntityUid entity, out EntityUid mindId, out MindComponent mind)
    {
        return TryGetMind(entity, out mindId, out mind) &&
               IsMindPrisoner(mindId, mind);
    }

    private bool TryGetDamageSourceMind(EntityUid? source, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        if (source == null)
            return false;

        if (TryGetMind(source.Value, out mindId, out mind))
            return true;

        if (TryGetProjectileSourceMind(source.Value, out mindId, out mind))
            return true;

        if (TryGetThrownItemSourceMind(source.Value, out mindId, out mind))
            return true;

        var current = source.Value;
        for (var i = 0; i < SourceParentSearchDepth; i++)
        {
            if (!TryComp(current, out TransformComponent? transform))
                return false;

            var parent = transform.ParentUid;
            if (parent == current)
                return false;

            if (TryGetMind(parent, out mindId, out mind))
                return true;

            if (TryGetProjectileSourceMind(parent, out mindId, out mind))
                return true;

            if (TryGetThrownItemSourceMind(parent, out mindId, out mind))
                return true;

            current = parent;
        }

        return false;
    }

    private bool TryGetProjectileSourceMind(EntityUid uid, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        if (!TryComp<ProjectileComponent>(uid, out var projectile))
            return false;

        if (projectile.Shooter != null && TryGetMind(projectile.Shooter.Value, out mindId, out mind))
            return true;

        return projectile.Weapon != null &&
               TryGetMind(projectile.Weapon.Value, out mindId, out mind);
    }

    private bool TryGetThrownItemSourceMind(EntityUid uid, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        return TryComp<ThrownItemComponent>(uid, out var thrown) &&
               thrown.Thrower != null &&
               TryGetMind(thrown.Thrower.Value, out mindId, out mind);
    }

    private bool TryGetMind(EntityUid uid, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) ||
            mindContainer.Mind == null)
        {
            return false;
        }

        var mindEntity = mindContainer.Mind.Value;
        if (!TryComp<MindComponent>(mindEntity, out var mindComponent))
            return false;

        mindId = mindEntity;
        mind = mindComponent;
        return true;
    }

    private bool TryGetLargestPrisonDamageContributor(
        EntityUid target,
        EntityUid targetMindId,
        out EntityUid sourceMindId,
        out MindComponent sourceMind)
    {
        sourceMindId = default;
        sourceMind = default!;

        if (!_prisonDamageByTarget.TryGetValue(target, out var sources))
            return false;

        var highest = FixedPoint2.Zero;
        var found = false;

        foreach (var (candidateMindId, damage) in sources)
        {
            MindComponent? candidateMind = null;
            if (candidateMindId == targetMindId ||
                damage <= highest ||
                !Resolve(candidateMindId, ref candidateMind, false) ||
                !IsMindPrisoner(candidateMindId, candidateMind))
            {
                continue;
            }

            sourceMindId = candidateMindId;
            sourceMind = candidateMind;
            highest = damage;
            found = true;
        }

        return found;
    }

    private bool TryGetLargestPrisonFaunaDamageContributor(
        EntityUid target,
        out EntityUid sourceMindId,
        out MindComponent sourceMind)
    {
        sourceMindId = default;
        sourceMind = default!;

        if (!_prisonFaunaDamageByTarget.TryGetValue(target, out var sources))
            return false;

        var highest = FixedPoint2.Zero;
        var found = false;
        foreach (var (candidateMindId, damage) in sources)
        {
            MindComponent? candidateMind = null;
            if (damage <= highest ||
                !Resolve(candidateMindId, ref candidateMind, false) ||
                !IsMindPrisoner(candidateMindId, candidateMind))
            {
                continue;
            }

            sourceMindId = candidateMindId;
            sourceMind = candidateMind;
            highest = damage;
            found = true;
        }

        return found;
    }

    private void ReducePrisonDamageContributors(EntityUid target, FixedPoint2 healing)
    {
        ReduceDamageContributors(_prisonDamageByTarget, target, healing);
    }

    private static void ReduceDamageContributors(
        Dictionary<EntityUid, Dictionary<EntityUid, FixedPoint2>> damageByTarget,
        EntityUid target,
        FixedPoint2 healing)
    {
        if (healing <= FixedPoint2.Zero || !damageByTarget.TryGetValue(target, out var sources))
            return;

        var totalTrackedDamage = FixedPoint2.Zero;
        foreach (var damage in sources.Values)
        {
            if (damage > FixedPoint2.Zero)
                totalTrackedDamage += damage;
        }

        if (totalTrackedDamage <= healing)
        {
            damageByTarget.Remove(target);
            return;
        }

        var sourceMindIds = new EntityUid[sources.Count];
        sources.Keys.CopyTo(sourceMindIds, 0);

        foreach (var sourceMindId in sourceMindIds)
        {
            var damage = sources[sourceMindId];
            var reduction = damage / totalTrackedDamage * healing;
            var remaining = damage - reduction;
            if (remaining <= FixedPoint2.Zero)
                sources.Remove(sourceMindId);
            else
                sources[sourceMindId] = remaining;
        }

        if (sources.Count == 0)
            damageByTarget.Remove(target);
    }

    private async void AddPrisonMurderPenalty(MindComponent killerMind)
    {
        if (killerMind.UserId is not { } userId)
            return;

        var minutes = Math.Max(1, _murderPenaltyMinutes);
        var now = DateTimeOffset.UtcNow;
        var expiration = now + TimeSpan.FromMinutes(minutes);
        var roundIds = _gameTicker.RoundId != 0
            ? ImmutableArray.Create(_gameTicker.RoundId)
            : ImmutableArray<int>.Empty;

        try
        {
            if (_player.TryGetSessionById(userId, out var session))
            {
                var check = CreateBanRefreshCheck(session);
                var bans = await _db.GetBansAsync(
                    check.Address,
                    check.UserId,
                    check.HwId,
                    check.ModernHwIds,
                    includeUnbanned: false);

                var latestBan = GetLatestActiveServerBan(bans);
                if (latestBan == null || !IsPrisonServerBan(latestBan))
                    return;

                if (IsPermanentPrisonBan(latestBan) && latestBan.Id is { } permanentBanId)
                {
                    await _db.SetBanPrisonAccess(permanentBanId, false);
                    _taskManager.RunOnMainThread(() => RevokePermanentPrisonAccess(userId));
                    return;
                }

                if (latestBan.ExpirationTime is { } activeExpiration &&
                    activeExpiration > now)
                {
                    expiration = activeExpiration + TimeSpan.FromMinutes(minutes);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to apply prison murder penalty for {userId}: {e}");
            return;
        }

        var ban = new BanDef(
            null,
            BanType.Server,
            ImmutableArray.Create(userId),
            ImmutableArray<(IPAddress address, int cidrMask)>.Empty,
            ImmutableArray<ImmutableTypedHwid>.Empty,
            now,
            expiration,
            roundIds,
            TimeSpan.Zero,
            Loc.GetString("prison-murder-penalty-reason"),
            NoteSeverity.High,
            null,
            null,
            sendToPrison: true);

        try
        {
            await _db.AddBanAsync(ban);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to add prison murder penalty for {userId}: {e}");
            return;
        }

        _taskManager.RunOnMainThread(() => ApplyPrisonMurderPenalty(userId, minutes));
    }

    private void ApplyPrisonMurderPenalty(NetUserId userId, int minutes)
    {
        _nextActiveBanRefresh = TimeSpan.Zero;

        if (_player.TryGetSessionById(userId, out var session))
        {
            _chat.DispatchServerMessage(
                session,
                Loc.GetString("prison-murder-penalty-message", ("minutes", minutes)));
        }
    }

    private void RevokePermanentPrisonAccess(NetUserId userId)
    {
        _nextActiveBanRefresh = TimeSpan.Zero;

        if (!_player.TryGetSessionById(userId, out var session))
            return;

        ClearPrisonState(session);
        session.Channel.Disconnect(Loc.GetString("prison-murder-permanent-message"));
    }

    private bool IsUserCurrentlyAntagonist(NetUserId userId)
    {
        return _mind.TryGetMind(userId, out var mindId, out _)
               && _role.MindIsAntagonist(mindId);
    }

    private bool IsSessionAntagonist(ICommonSession session)
    {
        return _mind.TryGetMind(session, out var mindId, out _)
               && _role.MindIsAntagonist(mindId);
    }

    private bool TryGetSpawnCoordinates(out EntityCoordinates coordinates)
    {
        var spawns = new List<EntityCoordinates>();

        var query = EntityQueryEnumerator<PrisonSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapID == MapId.Nullspace)
                continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            coordinates = EntityCoordinates.Invalid;
            return false;
        }

        coordinates = _random.Pick(spawns);
        return true;
    }

    public bool IsPrisonMap(MapId mapId)
    {
        var query = EntityQueryEnumerator<PrisonSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapID == mapId)
                return true;
        }

        return false;
    }

    private void SendPrisonMessage(ICommonSession session, BanDef ban)
    {
        if (ban.ExpirationTime == null)
        {
            _chat.DispatchServerMessage(session, Loc.GetString("prison-sent-permanent-message"));
            return;
        }

        var remaining = ban.ExpirationTime - DateTimeOffset.UtcNow;
        var minutes = remaining is { TotalMinutes: > 0 }
            ? Math.Ceiling(remaining.Value.TotalMinutes).ToString("N0")
            : "0";

        _chat.DispatchServerMessage(session, Loc.GetString("prison-sent-message", ("minutes", minutes)));
    }

    private static bool IsActiveServerBan(BanDef ban)
    {
        return ban.Type == BanType.Server
               && ban.Unban == null
               && (ban.ExpirationTime == null || ban.ExpirationTime > DateTimeOffset.UtcNow);
    }

    private static bool IsPrisonServerBan(BanDef ban)
    {
        return IsActiveServerBan(ban) && ban.SendToPrison;
    }

    private static bool IsPermanentPrisonBan(BanDef ban)
    {
        return IsPrisonServerBan(ban) && ban.ExpirationTime == null;
    }

    private static BanDef? GetLatestActiveServerBan(IEnumerable<BanDef> bans)
    {
        return bans
            .Where(IsActiveServerBan)
            .OrderByDescending(ban => ban.BanTime)
            .ThenByDescending(ban => ban.Id)
            .FirstOrDefault();
    }

    private readonly record struct PrisonBanRefreshCheck(
        NetUserId UserId,
        IPAddress Address,
        ImmutableArray<byte>? HwId,
        ImmutableArray<ImmutableArray<byte>> ModernHwIds);

    private readonly record struct PendingFaunaReward(
        PrisonBanRefreshCheck Check,
        int Minutes);

    private readonly record struct PrisonBanRefreshResult(
        NetUserId UserId,
        BanDef? LatestBan);
}

[ByRefEvent]
public readonly record struct PrisonerRegisteredEvent(ICommonSession Session);

public readonly record struct PrisonSentence(int BanId);
