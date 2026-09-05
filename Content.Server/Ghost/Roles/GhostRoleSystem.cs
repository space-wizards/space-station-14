using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Ghost.Roles;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Ghost.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles;

[UsedImplicitly]
public sealed partial class GhostRoleSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IBanManager _ban = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EuiManager _euiManager = default!;
    [Dependency] private FollowerSystem _followerSystem = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private SharedRoleSystem _roleSystem = default!;
    [Dependency] private TransformSystem _transform = default!;

    [Dependency] private EntityQuery<ActorComponent> _actorQuery;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery;
    [Dependency] private EntityQuery<GhostRoleRaffleComponent> _ghostRaffleQuery;
    [Dependency] private EntityQuery<GhostRoleComponent> _ghostRoleQuery;
    [Dependency] private EntityQuery<GhostTakeoverAvailableComponent> _ghostTakeoverQuery;
    [Dependency] private EntityQuery<MindComponent> _mindQuery;
    [Dependency] private EntityQuery<MindContainerComponent> _mindContainerQuery;
    [Dependency] private EntityQuery<MindRoleComponent> _mindRoleQuery;

    private bool _needsUpdateGhostRoleCount = true;

    private readonly Dictionary<ICommonSession, GhostRolesEui> _openUis = new();
    private readonly Dictionary<ICommonSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<GhostRoleComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive && !ent.Comp.Taken)
            EnsureComp<GhostTakeoverAvailableComponent>(ent);
        else if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            RemComp<GhostTakeoverAvailableComponent>(ent);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
    }

    public void OpenEui(ICommonSession session)
    {
        if (session.AttachedEntity is not { Valid: true } attached ||
            !_ghostQuery.HasComp(attached))
            return;

        if (_openUis.ContainsKey(session))
            CloseEui(session);

        var eui = _openUis[session] = new GhostRolesEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void OpenMakeGhostRoleEui(ICommonSession session, EntityUid uid)
    {
        if (session.AttachedEntity == null)
            return;

        if (_openMakeGhostRoleUis.ContainsKey(session))
            CloseEui(session);

        var eui = _openMakeGhostRoleUis[session] = new MakeGhostRoleEui(EntityManager, GetNetEntity(uid));
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void CloseEui(ICommonSession session)
    {
        if (!_openUis.ContainsKey(session))
            return;

        _openUis.Remove(session, out var eui);

        eui?.Close();
    }

    public void CloseMakeGhostRoleEui(ICommonSession session)
    {
        if (_openMakeGhostRoleUis.Remove(session, out var eui))
        {
            eui.Close();
        }
    }

    public void UpdateAllEui()
    {
        foreach (var eui in _openUis.Values)
        {
            eui.StateDirty();
        }
        // Note that this, like the EUIs, is deferred.
        // This is for roughly the same reasons, too:
        // Someone might spawn a ton of ghost roles at once.
        _needsUpdateGhostRoleCount = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGhostRoleCount();
        UpdateRaffles(frameTime);
    }

    /// <summary>
    /// Handles sending count update for the ghost role button in ghost UI, if ghost role count changed.
    /// </summary>
    private void UpdateGhostRoleCount()
    {
        if (!_needsUpdateGhostRoleCount)
            return;

        _needsUpdateGhostRoleCount = false;
        var response = new GhostUpdateGhostRoleCountEvent(GetGhostRoleCount());
        foreach (var player in _playerManager.Sessions)
        {
            RaiseNetworkEvent(response, player.Channel);
        }
    }

    /// <summary>
    /// Handles ghost role raffle logic.
    /// </summary>
    private void UpdateRaffles(float frameTime)
    {
        var query = EntityQueryEnumerator<GhostRoleRaffleComponent, MetaDataComponent>();
        while (query.MoveNext(out var entityUid, out var raffle, out var meta))
        {
            if (meta.EntityPaused)
                continue;

            // if all participants leave/were removed from the raffle, the raffle is canceled.
            if (raffle.CurrentMembers.Count == 0)
            {
                RemoveRaffleAndUpdateEui(entityUid, raffle);
                continue;
            }

            raffle.Countdown = raffle.Countdown.Subtract(TimeSpan.FromSeconds(frameTime));
            if (raffle.Countdown.Ticks > 0)
                continue;

            // the raffle is over! find someone to take over the ghost role
            if (!_ghostRoleQuery.TryComp(entityUid, out var ghostRole))
            {
                Log.Warning($"Ghost role raffle finished on {entityUid} but {nameof(GhostRoleComponent)} is missing");
                RemoveRaffleAndUpdateEui(entityUid, raffle);
                continue;
            }

            if (ghostRole.RaffleConfig is null)
            {
                Log.Warning($"Ghost role raffle finished on {entityUid} but RaffleConfig became null");
                RemoveRaffleAndUpdateEui(entityUid, raffle);
                continue;
            }

            var foundWinner = false;
            var deciderPrototype = ProtoMan.Index(ghostRole.RaffleConfig.Decider);

            // use the ghost role's chosen winner picker to find a winner
            deciderPrototype.Decider.PickWinner(
                raffle.CurrentMembers.AsEnumerable(),
                session =>
                {
                    var success = TryTakeover(session, GetNetEntity(entityUid));
                    foundWinner |= success;
                    return success;
                }
            );

            if (!foundWinner)
            {
                Log.Warning($"Ghost role raffle for {entityUid} ({ghostRole.RoleName}) finished without " +
                            $"{ghostRole.RaffleConfig?.Decider} finding a winner");
            }

            // raffle over
            RemoveRaffleAndUpdateEui(entityUid, raffle);
        }
    }

    private bool TryTakeover(ICommonSession player, NetEntity identifier)
    {
        // Can't win if you are disconnected (although you shouldn't be a candidate anyway)
        if (player.Status != SessionStatus.InGame)
            return false;

        // Can't win if you are no longer a ghost (e.g. if you returned to your body)
        if (!_ghostQuery.HasComp(player.AttachedEntity))
            return false;

        if (Takeover(player, identifier))
        {
            // takeover successful, we have a winner! remove the winner from other raffles they might be in
            LeaveAllRaffles(player);
            return true;
        }

        return false;
    }

    private void RemoveRaffleAndUpdateEui(EntityUid entityUid, GhostRoleRaffleComponent raffle)
    {
        RemComp(entityUid, raffle);
        UpdateAllEui();
    }

    private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.InGame)
        {
            var response = new GhostUpdateGhostRoleCountEvent(Count<GhostTakeoverAvailableComponent>());
            RaiseNetworkEvent(response, args.Session.Channel);
        }
        else
        {
            // people who disconnect are removed from ghost role raffles
            LeaveAllRaffles(args.Session);
        }
    }

    public void RegisterGhostRole(Entity<GhostRoleComponent> role)
    {
        if (role.Comp.Taken)
            return;

        EnsureComp<GhostTakeoverAvailableComponent>(role);
        if (role.Comp.RaffleConfig != null)
            EnsureComp<GhostRoleRaffleComponent>(role);
        UpdateAllEui();
    }

    public void UnregisterGhostRole(Entity<GhostRoleComponent> role)
    {
        var hadTakeover = RemComp<GhostTakeoverAvailableComponent>(role);
        if (_ghostRaffleQuery.TryComp(role, out var raffle))
        {
            // if a raffle is still running, get rid of it
            RemoveRaffleAndUpdateEui(role.Owner, raffle);
        }
        else if (hadTakeover)
        {
            UpdateAllEui();
        }
    }

    // probably fine to be init because it's never added during entity initialization, but much later
    [SubscribeLocalEvent]
    private void OnTakeoverInit(Entity<GhostTakeoverAvailableComponent> ent, ref ComponentInit args)
    {
        if (!_ghostRoleQuery.TryComp(ent, out var ghostRole)
            || ghostRole.Taken)
            RemComp(ent, ent.Comp);
    }

    // probably fine to be init because it's never added during entity initialization, but much later
    [SubscribeLocalEvent]
    private void OnRaffleInit(Entity<GhostRoleRaffleComponent> ent, ref ComponentInit args)
    {
        if (!_ghostRoleQuery.TryComp(ent, out var ghostRole))
        {
            RemComp(ent, ent.Comp); // Ghost role doesn't exist.
            return;
        }

        var config = ghostRole.RaffleConfig;
        if (config is null)
        {
            RemComp(ent, ent.Comp); // No raffle settings.
            return;
        }

        var settings = config.SettingsOverride
                       ?? ProtoMan.Index(config.Settings).Settings;

        if (settings.MaxDuration < settings.InitialDuration)
        {
            Log.Error($"Ghost role on {ent} has invalid raffle settings (max duration shorter than initial)");
            ghostRole.RaffleConfig = null; // make it a non-raffle role so stuff isn't entirely broken
            RemComp<GhostRoleRaffleComponent>(ent);
            return;
        }

        var raffle = ent.Comp;
        var countdown = _cfg.GetCVar(CCVars.GhostQuickLottery) ? 1 : settings.InitialDuration;
        raffle.Countdown = TimeSpan.FromSeconds(countdown);
        raffle.CumulativeTime = TimeSpan.FromSeconds(settings.InitialDuration);
        // we copy these settings into the component because they would be cumbersome to access otherwise
        raffle.JoinExtendsDurationBy = TimeSpan.FromSeconds(settings.JoinExtendsDurationBy);
        raffle.MaxDuration = TimeSpan.FromSeconds(settings.MaxDuration);
    }

    /// <summary>
    /// Joins the given player onto a ghost role raffle, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="identifier">The ID that represents the ghost role or ghost role raffle.
    /// (A raffle will have the same ID as the ghost role it's for.)</param>
    private void JoinRaffle(ICommonSession player, NetEntity entity)
    {
        var ghostUid = GetEntity(entity);
        if (!_ghostTakeoverQuery.HasComp(ghostUid))
            return;

        // get raffle or create a new one if it doesn't exist
        var raffle = EnsureComp<GhostRoleRaffleComponent>(ghostUid);

        if (!raffle.CurrentMembers.Add(player))
        {
            Log.Warning($"{player.Name} tried to join raffle for ghost role {ghostUid} but they are already in the raffle");
            return;
        }

        // if this is the first time the player joins this raffle, and the player wasn't the starter of the raffle:
        // extend the countdown, but only if doing so will not make the raffle take longer than the maximum
        // duration
        if (raffle.AllMembers.Add(player) && raffle.AllMembers.Count > 1
            && raffle.CumulativeTime.Add(raffle.JoinExtendsDurationBy) <= raffle.MaxDuration)
        {
            raffle.Countdown += raffle.JoinExtendsDurationBy;
            raffle.CumulativeTime += raffle.JoinExtendsDurationBy;
        }

        UpdateAllEui();
    }

    /// <summary>
    /// Makes the given player leave the raffle corresponding to the given ID.
    /// </summary>
    public void LeaveRaffle(ICommonSession player, NetEntity identifier)
    {
        var roleUid = GetEntity(identifier);
        if (!_ghostRaffleQuery.TryComp(roleUid, out var raffleComp))
            return;

        if (raffleComp.CurrentMembers.Remove(player))
        {
            UpdateAllEui();
        }
        else
        {
            Log.Warning($"{player.Name} tried to leave raffle for ghost role {roleUid} but they are not in the raffle");
        }

        // (raffle ending because all players left is handled in update())
    }

    /// <summary>
    /// Makes the given player leave all ghost role raffles.
    /// </summary>
    public void LeaveAllRaffles(ICommonSession player)
    {
        var shouldUpdateEui = false;

        var raffles = EntityQueryEnumerator<GhostRoleRaffleComponent>();

        while (raffles.MoveNext(out var raffleComp))
        {
            shouldUpdateEui |= raffleComp.CurrentMembers.Remove(player);
        }

        if (shouldUpdateEui)
            UpdateAllEui();
    }

    /// <summary>
    /// Request a ghost role. If it's a raffled role starts or joins a raffle, otherwise the player immediately
    /// takes over the ghost role if possible.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="identifier">ID of the ghost role.</param>
    public void Request(ICommonSession player, NetEntity identifier)
    {
        var roleUid = GetEntity(identifier);
        if (!_ghostTakeoverQuery.HasComp(roleUid)
            || !_ghostRoleQuery.TryComp(roleUid, out var ghostRole))
            return;

        Entity<GhostRoleComponent> roleEnt = (roleUid, ghostRole);

        TryPrototypes(roleEnt, out var antags, out var jobs);

        // Check role bans
        if (_ban.IsRoleBanned(player, antags) || _ban.IsRoleBanned(player, jobs))
        {
            Log.Warning($"Server rejected ghost role request '{roleEnt.Comp.RoleName}' for '{player.Name}' - client missed ban?");
            return;
        }

        // Check role requirements
        if (!IsRoleAllowed(player, jobs, antags))
        {
            Log.Warning($"Server rejected ghost role request '{roleEnt.Comp.RoleName}' for '{player.Name}' - client missed requirement check?");
            return;
        }

        // Decide to do a raffle or not
        if (roleEnt.Comp.RaffleConfig is not null)
        {
            JoinRaffle(player, identifier);
        }
        else
        {
            Takeover(player, identifier);
        }
    }

    /// <summary>
    /// Collect all role prototypes on the Ghostrole.
    /// </summary>
    /// <returns>
    /// Returns true if at least on role prototype could be found.
    /// </returns>
    private bool TryPrototypes(
        Entity<GhostRoleComponent> roleEnt,
        out List<ProtoId<AntagPrototype>> antags,
        out List<ProtoId<JobPrototype>> jobs)
    {
        antags = [];
        jobs = [];

        // If there is a mind already, check its mind roles.
        // Not sure if this can ever actually happen.
        if (_mindContainerQuery.TryComp(roleEnt, out var mindCont)
            && _mindQuery.TryComp(mindCont.Mind, out var mind))
        {
            foreach (var role in mind.MindRoleContainer.ContainedEntities)
            {
                if (!_mindRoleQuery.TryComp(role, out var comp))
                    continue;

                if (comp.JobPrototype is not null)
                    jobs.Add(comp.JobPrototype.Value);

                else if (comp.AntagPrototype is not null)
                    antags.Add(comp.AntagPrototype.Value);
            }

            return antags.Count > 0 || jobs.Count > 0;
        }

        if (roleEnt.Comp.JobProto is not null)
            jobs.Add(roleEnt.Comp.JobProto.Value);


        // If there is no mind, check the mindRole prototypes
        foreach (var proto in roleEnt.Comp.MindRoles)
        {
            if (!ProtoMan.TryIndex(proto, out var indexed)
                || !indexed.TryComp<MindRoleComponent>(out var roleComp, Factory))
                continue;

            if (roleComp.JobPrototype is not null)
                jobs.Add(roleComp.JobPrototype.Value);
            else if (roleComp.AntagPrototype is not null)
                antags.Add(roleComp.AntagPrototype.Value);
            else
                Log.Debug($"Mind role '{proto}' of '{roleEnt.Comp.RoleName}' has neither a job or antag prototype specified");
        }

        return antags.Count > 0 || jobs.Count > 0;
    }

    /// <summary>
    /// Checks if the player passes the requirements for the supplied roles.
    /// Returns false if any role fails the check.
    /// </summary>
    private bool IsRoleAllowed(
        ICommonSession player,
        List<ProtoId<JobPrototype>>? jobIds,
        List<ProtoId<AntagPrototype>>? antagIds)
    {
        var ev = new IsRoleAllowedEvent(player, jobIds, antagIds);
        RaiseLocalEvent(ref ev);

        return !ev.Cancelled;
    }

    /// <summary>
    /// Attempts having the player take over the ghost role with the corresponding ID. Does not start a raffle.
    /// </summary>
    /// <returns>True if takeover was successful, otherwise false.</returns>
    public bool Takeover(ICommonSession player, NetEntity identifier)
    {
        var role = GetEntity(identifier);
        if (!_ghostTakeoverQuery.HasComp(role)
            || !_ghostRoleQuery.TryComp(role, out var ghostRole))
            return false;

        var ev = new TakeGhostRoleEvent(player);
        RaiseLocalEvent(role, ref ev);

        if (!ev.TookRole)
            return false;

        if (player.AttachedEntity != null)
            _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {ghostRole.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");

        CloseEui(player);
        return true;
    }

    public void Follow(ICommonSession player, NetEntity identifier)
    {
        var role = GetEntity(identifier);
        if (!_ghostTakeoverQuery.HasComp(role))
            return;

        if (player.AttachedEntity == null)
            return;

        _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, role);
    }

    public void GhostRoleInternalCreateMindAndTransfer(ICommonSession player, EntityUid roleUid, EntityUid mob, GhostRoleComponent? role = null)
    {
        if (!Resolve(roleUid, ref role))
            return;

        DebugTools.AssertNotNull(player.ContentData());

        // After taking a ghost role, the player cannot return to the original body, so wipe the player's current mind
        // unless it is a visiting mind
        if (_mindSystem.TryGetMind(player.UserId, out _, out var mind) && !mind.IsVisitingEntity)
            _mindSystem.WipeMind(player);

        var newMind = _mindSystem.CreateMind(player.UserId,
            Comp<MetaDataComponent>(mob).EntityName);

        _mindSystem.SetUserId(newMind, player.UserId);
        _mindSystem.TransferTo(newMind, mob);

        _roleSystem.MindAddRoles(newMind.Owner, role.MindRoles, newMind.Comp);
    }

    /// <summary>
    /// Returns the number of available ghost roles.
    /// </summary>
    public int GetGhostRoleCount()
    {
        // Count includes paused objects, so we need to use an EntityQueryEnumerator.
        var takeoverQuery = EntityQueryEnumerator<GhostTakeoverAvailableComponent>();
        var output = 0;
        while (takeoverQuery.MoveNext(out _, out _))
            output++;

        return output;
    }

    /// <summary>
    /// Returns information about all available ghost roles.
    /// </summary>
    /// <param name="player">
    /// If not null, the <see cref="GhostRoleInfo"/>s will show if the given player is in a raffle.
    /// </param>
    public GhostRoleInfo[] GetGhostRolesInfo(ICommonSession? player)
    {
        var roles = new List<GhostRoleInfo>();

        var takeoverQuery = EntityQueryEnumerator<GhostTakeoverAvailableComponent, GhostRoleComponent>();
        while (takeoverQuery.MoveNext(out var uid, out _, out var role))
        {
            if (MetaData(uid).EntityPaused)
                continue;


            var kind = GhostRoleKind.FirstComeFirstServe;
            GhostRoleRaffleComponent? raffle = null;

            if (role.RaffleConfig is not null)
            {
                kind = GhostRoleKind.RaffleReady;

                if (_ghostRaffleQuery.TryComp(uid, out var raffleComp))
                {
                    kind = GhostRoleKind.RaffleInProgress;

                    if (player is not null && raffleComp.CurrentMembers.Contains(player))
                        kind = GhostRoleKind.RaffleJoined;
                }
            }

            var rafflePlayerCount = (uint?)raffle?.CurrentMembers.Count ?? 0;
            var raffleEndTime = raffle is not null
                ? _timing.CurTime.Add(raffle.Countdown)
                : TimeSpan.MinValue;

            TryPrototypes((uid, role), out var antags, out var jobs);

            roles.Add(new GhostRoleInfo
            {
                Identifier = GetNetEntity(uid),
                Name = role.RoleName,
                Description = role.RoleDescription,
                Rules = role.RoleRules,
                RolePrototypes = (jobs, antags),
                Kind = kind,
                RafflePlayerCount = rafflePlayerCount,
                RaffleEndTime = raffleEndTime
            });
        }

        return roles.ToArray();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(PlayerAttachedEvent message)
    {
        // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
        if (!_openUis.ContainsKey(message.Player))
            return;

        if (_ghostQuery.HasComp(message.Entity))
            return;

        // The player is not a ghost (anymore), so they should not be in any raffles. Remove them.
        // This ensures player doesn't win a raffle after returning to their (revived) body and ends up being
        // forced into a ghost role.
        LeaveAllRaffles(message.Player);
        CloseEui(message.Player);
    }

    [SubscribeLocalEvent]
    private void OnMindAdded(EntityUid uid, GhostRoleComponent component, MindAddedMessage args)
    {
        if (component.JobProto != null)
        {
            _roleSystem.MindAddJobRole(args.Mind, args.Mind, silent: false, component.JobProto);
        }

        component.Taken = true;
        UnregisterGhostRole((uid, component));
    }

    [SubscribeLocalEvent]
    private void OnMindRemoved(EntityUid uid, GhostRoleComponent component, MindRemovedMessage args)
    {
        // Avoid re-registering it for duplicate entries and potential exceptions.
        if (!component.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
            return;

        component.Taken = false;
        RegisterGhostRole((uid, component));
    }

    [SubscribeLocalEvent]
    public void Reset(RoundRestartCleanupEvent ev)
    {
        foreach (var session in _openUis.Keys)
        {
            CloseEui(session);
        }

        _openUis.Clear();
    }

    [SubscribeLocalEvent]
    private void OnPaused(Entity<GhostRoleComponent> ent, ref EntityPausedEvent args)
    {
        if (_actorQuery.HasComp(ent))
            return;

        UpdateAllEui();
    }

    [SubscribeLocalEvent]
    private void OnUnpaused(Entity<GhostRoleComponent> ent, ref EntityUnpausedEvent args)
    {
        if (_actorQuery.HasComp(ent))
            return;

        UpdateAllEui();
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GhostRoleComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Probability < 1f && !_random.Prob(ent.Comp.Probability))
        {
            RemCompDeferred<GhostRoleComponent>(ent);
            return;
        }

        if (ent.Comp.Taken)
            UnregisterGhostRole(ent);
        else
            RegisterGhostRole(ent);
    }

    [SubscribeLocalEvent]
    private void OnRoleShutdown(Entity<GhostRoleComponent> ent, ref ComponentShutdown args)
    {
        UnregisterGhostRole(ent);
    }

    [SubscribeLocalEvent]
    private void OnSpawnerTakeRole(Entity<GhostRoleMobSpawnerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (!_ghostRoleQuery.TryComp(ent, out var ghostRole) ||
            !CanTakeGhost((ent, ghostRole)))
        {
            args.TookRole = false;
            return;
        }

        if (string.IsNullOrEmpty(ent.Comp.Prototype))
            throw new NullReferenceException("Prototype string cannot be null or empty!");

        if (!_transform.TryGetMapOrGridCoordinates(ent, out var spawnCoordinates))
            return;

        var mob = Spawn(ent.Comp.Prototype, spawnCoordinates.Value);

        var spawnedEvent = new GhostRoleSpawnerUsedEvent(ent, mob);
        RaiseLocalEvent(mob, ref spawnedEvent);

        if (ghostRole.MakeSentient)
            _mindSystem.MakeSentient(mob, ghostRole.AllowMovement, ghostRole.AllowSpeech);

        EnsureComp<MindContainerComponent>(mob);

        GhostRoleInternalCreateMindAndTransfer(args.Player, ent, mob, ghostRole);

        if (++ent.Comp.CurrentTakeovers < ent.Comp.AvailableTakeovers)
        {
            args.TookRole = true;
            return;
        }

        ghostRole.Taken = true;

        if (ent.Comp.DeleteOnSpawn)
            QueueDel(ent);

        args.TookRole = true;
    }

    private bool CanTakeGhost(Entity<GhostRoleComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) &&
               !ent.Comp.Taken &&
               !MetaData(ent).EntityPaused;
    }

    [SubscribeLocalEvent]
    private void OnTakeRole(Entity<GhostRoleComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (!CanTakeGhost(ent.AsNullable()))
        {
            args.TookRole = false;
            return;
        }

        ent.Comp.Taken = true;

        var mind = EnsureComp<MindContainerComponent>(ent);

        if (mind.HasMind)
        {
            args.TookRole = false;
            return;
        }

        if (ent.Comp.MakeSentient)
            _mindSystem.MakeSentient(ent, ent.Comp.AllowMovement, ent.Comp.AllowSpeech);

        GhostRoleInternalCreateMindAndTransfer(args.Player, ent, ent, ent.Comp);
        UnregisterGhostRole(ent);

        args.TookRole = true;
    }

    [SubscribeLocalEvent]
    private void OnVerb(Entity<GhostRoleMobSpawnerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var prototypes = ent.Comp.SelectablePrototypes;
        if (prototypes.Count < 1)
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var verbs = new ValueList<Verb>();

        foreach (var ghostProtoId in prototypes)
        {
            if (!ProtoMan.TryIndex(ghostProtoId, out var ghostProto))
                continue;

            var verb = CreateVerb(ent, args.User, ghostProto);
            verbs.Add(verb);
        }

        args.Verbs.UnionWith(verbs);
    }

    private Verb CreateVerb(Entity<GhostRoleMobSpawnerComponent> ent, EntityUid userUid, GhostRolePrototype prototype)
    {
        var verbText = Loc.GetString(prototype.Name);

        return new Verb()
        {
            Text = verbText,
            Disabled = ent.Comp.Prototype == prototype.EntityPrototype,
            Category = VerbCategory.SelectType,
            Act = () => SetMode(ent, prototype, verbText, ent.Comp, userUid)
        };
    }

    public void SetMode(EntityUid uid, GhostRolePrototype prototype, string verbText, GhostRoleMobSpawnerComponent? component, EntityUid? userUid = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var ghostrolecomp = EnsureComp<GhostRoleComponent>(uid);

        component.Prototype = prototype.EntityPrototype;
        ghostrolecomp.RoleName = verbText;
        ghostrolecomp.RoleDescription = prototype.Description;
        ghostrolecomp.RoleRules = prototype.Rules;

        // Dirty(ghostrolecomp);

        if (userUid != null)
        {
            var msg = Loc.GetString("ghostrole-spawner-select", ("mode", verbText));
            _popupSystem.PopupEntity(msg, uid, userUid.Value);
        }
    }

    [SubscribeLocalEvent]
    public void OnGhostRoleRadioMessage(Entity<GhostRoleMobSpawnerComponent> entity, ref GhostRoleRadioMessage args)
    {
        if (!ProtoMan.Resolve(args.ProtoId, out var ghostRoleProto))
            return;

        // if the prototype chosen isn't actually part of the selectable options, ignore it
        foreach (var selectableProto in entity.Comp.SelectablePrototypes)
        {
            if (selectableProto == ghostRoleProto.EntityPrototype.Id)
                return;
        }

        SetMode(entity.Owner, ghostRoleProto, ghostRoleProto.Name, entity.Comp);
    }
}

[AnyCommand]
public sealed partial class GhostRoles : IConsoleCommand
{
    [Dependency] private IEntityManager _e = default!;

    public string Command => "ghostroles";
    public string Description => "Opens the ghost role request window.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player != null)
            _e.System<GhostRoleSystem>().OpenEui(shell.Player);
        else
            shell.WriteLine("You can only open the ghost roles UI on a client.");
    }
}
