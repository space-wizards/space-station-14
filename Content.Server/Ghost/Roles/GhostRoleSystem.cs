using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Collections;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server.Ghost.Roles;

[UsedImplicitly]
public sealed class GhostRoleSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private uint _nextRoleIdentifier;
    private bool _needsUpdateGhostRoleCount = true;

    private readonly Dictionary<uint, Entity<GhostRoleComponent>> _ghostRoles = new();
    private readonly Dictionary<uint, Entity<GhostRoleRaffleComponent>> _ghostRoleRaffles = new();

    private readonly Dictionary<ICommonSession, GhostRolesEui> _openUis = new();
    private readonly Dictionary<ICommonSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

    [ViewVariables]
    public IReadOnlyCollection<Entity<GhostRoleComponent>> GhostRoles => _ghostRoles.Values;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);

        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, TakeGhostRoleEvent>(OnTakeoverTakeRole);

        SubscribeLocalEvent<GhostRoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GhostRoleComponent, ComponentStartup>(OnRoleStartup);
        SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnRoleShutdown);
        SubscribeLocalEvent<GhostRoleComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<GhostRoleComponent, EntityUnpausedEvent>(OnUnpaused);

        SubscribeLocalEvent<GhostRoleRaffleComponent, ComponentInit>(OnRaffleInit);
        SubscribeLocalEvent<GhostRoleRaffleComponent, ComponentShutdown>(OnRaffleShutdown);

        SubscribeLocalEvent<GhostRoleMobSpawnerComponent, TakeGhostRoleEvent>(OnSpawnerTakeRole);
        SubscribeLocalEvent<GhostRoleMobSpawnerComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<GhostRoleMobSpawnerComponent, GhostRoleRadioMessage>(OnGhostRoleRadioMessage);
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    private void OnMobStateChanged(Entity<GhostTakeoverAvailableComponent> component, ref MobStateChangedEvent args)
    {
        if (!TryComp(component, out GhostRoleComponent? ghostRole))
            return;

        switch (args.NewMobState)
        {
            case MobState.Alive:
                {
                    if (!ghostRole.Taken)
                        RegisterGhostRole((component, ghostRole));
                    break;
                }
            case MobState.Critical:
            case MobState.Dead:
                UnregisterGhostRole((component, ghostRole));
                break;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
    }

    private uint GetNextRoleIdentifier()
    {
        return unchecked(_nextRoleIdentifier++);
    }

    public void OpenEui(ICommonSession session)
    {
        if (session.AttachedEntity is not { Valid: true } attached ||
            !EntityManager.HasComponent<GhostComponent>(attached))
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
            if (!TryComp(entityUid, out GhostRoleComponent? ghostRole))
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
            var deciderPrototype = _prototype.Index(ghostRole.RaffleConfig.Decider);

            // use the ghost role's chosen winner picker to find a winner
            deciderPrototype.Decider.PickWinner(
                raffle.CurrentMembers.AsEnumerable(),
                session =>
                {
                    var success = TryTakeover(session, raffle.Identifier);
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

    private bool TryTakeover(ICommonSession player, uint identifier)
    {
        // TODO: the following two checks are kind of redundant since they should already be removed
        //           from the raffle
        // can't win if you are disconnected (although you shouldn't be a candidate anyway)
        if (player.Status != SessionStatus.InGame)
            return false;

        // can't win if you are no longer a ghost (e.g. if you returned to your body)
        if (player.AttachedEntity == null || !HasComp<GhostComponent>(player.AttachedEntity))
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
        _ghostRoleRaffles.Remove(raffle.Identifier);
        RemComp(entityUid, raffle);
        UpdateAllEui();
    }

    private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.InGame)
        {
            var response = new GhostUpdateGhostRoleCountEvent(_ghostRoles.Count);
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
        if (_ghostRoles.ContainsValue(role))
            return;

        _ghostRoles[role.Comp.Identifier = GetNextRoleIdentifier()] = role;
        UpdateAllEui();
    }

    public void UnregisterGhostRole(Entity<GhostRoleComponent> role)
    {
        var comp = role.Comp;
        if (!_ghostRoles.ContainsKey(comp.Identifier) || _ghostRoles[comp.Identifier] != role)
            return;

        _ghostRoles.Remove(comp.Identifier);
        if (TryComp(role.Owner, out GhostRoleRaffleComponent? raffle))
        {
            // if a raffle is still running, get rid of it
            RemoveRaffleAndUpdateEui(role.Owner, raffle);
        }
        else
        {
            UpdateAllEui();
        }
    }

    // probably fine to be init because it's never added during entity initialization, but much later
    private void OnRaffleInit(Entity<GhostRoleRaffleComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent, out GhostRoleComponent? ghostRole))
        {
            // can't have a raffle for a ghost role that doesn't exist
            RemComp<GhostRoleRaffleComponent>(ent);
            return;
        }

        var config = ghostRole.RaffleConfig;
        if (config is null)
            return; // should, realistically, never be reached but you never know

        var settings = config.SettingsOverride
                       ?? _prototype.Index<GhostRoleRaffleSettingsPrototype>(config.Settings).Settings;

        if (settings.MaxDuration < settings.InitialDuration)
        {
            Log.Error($"Ghost role on {ent} has invalid raffle settings (max duration shorter than initial)");
            ghostRole.RaffleConfig = null; // make it a non-raffle role so stuff isn't entirely broken
            RemComp<GhostRoleRaffleComponent>(ent);
            return;
        }

        var raffle = ent.Comp;
        raffle.Identifier = ghostRole.Identifier;
        var countdown = _cfg.GetCVar(CCVars.GhostQuickLottery)? 1 : settings.InitialDuration;
        raffle.Countdown = TimeSpan.FromSeconds(countdown);
        raffle.CumulativeTime = TimeSpan.FromSeconds(settings.InitialDuration);
        // we copy these settings into the component because they would be cumbersome to access otherwise
        raffle.JoinExtendsDurationBy = TimeSpan.FromSeconds(settings.JoinExtendsDurationBy);
        raffle.MaxDuration = TimeSpan.FromSeconds(settings.MaxDuration);
    }

    private void OnRaffleShutdown(Entity<GhostRoleRaffleComponent> ent, ref ComponentShutdown args)
    {
        _ghostRoleRaffles.Remove(ent.Comp.Identifier);
    }

    /// <summary>
    /// Joins the given player onto a ghost role raffle, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="identifier">The ID that represents the ghost role or ghost role raffle.
    /// (A raffle will have the same ID as the ghost role it's for.)</param>
    private void JoinRaffle(ICommonSession player, uint identifier)
    {
        if (!_ghostRoles.TryGetValue(identifier, out var roleEnt))
            return;

        // get raffle or create a new one if it doesn't exist
        var raffle = _ghostRoleRaffles.TryGetValue(identifier, out var raffleEnt)
            ? raffleEnt.Comp
            : EnsureComp<GhostRoleRaffleComponent>(roleEnt.Owner);

        _ghostRoleRaffles.TryAdd(identifier, (roleEnt.Owner, raffle));

        if (!raffle.CurrentMembers.Add(player))
        {
            Log.Warning($"{player.Name} tried to join raffle for ghost role {identifier} but they are already in the raffle");
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
    public void LeaveRaffle(ICommonSession player, uint identifier)
    {
        if (!_ghostRoleRaffles.TryGetValue(identifier, out var raffleEnt))
            return;

        if (raffleEnt.Comp.CurrentMembers.Remove(player))
        {
            UpdateAllEui();
        }
        else
        {
            Log.Warning($"{player.Name} tried to leave raffle for ghost role {identifier} but they are not in the raffle");
        }

        // (raffle ending because all players left is handled in update())
    }

    /// <summary>
    /// Makes the given player leave all ghost role raffles.
    /// </summary>
    public void LeaveAllRaffles(ICommonSession player)
    {
        var shouldUpdateEui = false;

        foreach (var raffleEnt in _ghostRoleRaffles.Values)
        {
            shouldUpdateEui |= raffleEnt.Comp.CurrentMembers.Remove(player);
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
    public void Request(ICommonSession player, uint identifier)
    {
        if (!_ghostRoles.TryGetValue(identifier, out var roleEnt))
            return;

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
    /// Attempts having the player take over the ghost role with the corresponding ID. Does not start a raffle.
    /// </summary>
    /// <returns>True if takeover was successful, otherwise false.</returns>
    public bool Takeover(ICommonSession player, uint identifier)
    {
        if (!_ghostRoles.TryGetValue(identifier, out var role))
            return false;

        var ev = new TakeGhostRoleEvent(player);
        RaiseLocalEvent(role, ref ev);

        if (!ev.TookRole)
            return false;

        if (player.AttachedEntity != null)
            _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {role.Comp.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");

        CloseEui(player);
        return true;
    }

    public void Follow(ICommonSession player, uint identifier)
    {
        if (!_ghostRoles.TryGetValue(identifier, out var role))
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

        var newMind = _mindSystem.CreateMind(player.UserId,
            EntityManager.GetComponent<MetaDataComponent>(mob).EntityName);

        _roleSystem.MindAddRole(newMind, "MindRoleGhostMarker");

        if(_roleSystem.MindHasRole<GhostRoleMarkerRoleComponent>(newMind!, out var markerRole))
            markerRole.Value.Comp2.Name = role.RoleName;

        _mindSystem.SetUserId(newMind, player.UserId);
        _mindSystem.TransferTo(newMind, mob);
    }

    /// <summary>
    /// Returns the number of available ghost roles.
    /// </summary>
    public int GetGhostRoleCount()
    {
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        return _ghostRoles.Count(pair => metaQuery.GetComponent(pair.Value.Owner).EntityPaused == false);
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
        var metaQuery = GetEntityQuery<MetaDataComponent>();

        foreach (var (id, (uid, role)) in _ghostRoles)
        {
            if (metaQuery.GetComponent(uid).EntityPaused)
                continue;


            var kind = GhostRoleKind.FirstComeFirstServe;
            GhostRoleRaffleComponent? raffle = null;

            if (role.RaffleConfig is not null)
            {
                kind = GhostRoleKind.RaffleReady;

                if (_ghostRoleRaffles.TryGetValue(id, out var raffleEnt))
                {
                    kind = GhostRoleKind.RaffleInProgress;
                    raffle = raffleEnt.Comp;

                    if (player is not null && raffle.CurrentMembers.Contains(player))
                        kind = GhostRoleKind.RaffleJoined;
                }
            }

            var rafflePlayerCount = (uint?) raffle?.CurrentMembers.Count ?? 0;
            var raffleEndTime = raffle is not null
                ? _timing.CurTime.Add(raffle.Countdown)
                : TimeSpan.MinValue;

            roles.Add(new GhostRoleInfo
            {
                Identifier = id,
                Name = role.RoleName,
                Description = role.RoleDescription,
                Rules = role.RoleRules,
                Requirements = role.Requirements,
                Kind = kind,
                RafflePlayerCount = rafflePlayerCount,
                RaffleEndTime = raffleEndTime
            });
        }

        return roles.ToArray();
    }

    private void OnPlayerAttached(PlayerAttachedEvent message)
    {
        // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
        if (!_openUis.ContainsKey(message.Player))
            return;

        if (HasComp<GhostComponent>(message.Entity))
            return;

        // The player is not a ghost (anymore), so they should not be in any raffles. Remove them.
        // This ensures player doesn't win a raffle after returning to their (revived) body and ends up being
        // forced into a ghost role.
        LeaveAllRaffles(message.Player);
        CloseEui(message.Player);
    }

    private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
    {
        if (!TryComp(uid, out GhostRoleComponent? ghostRole))
            return;

        if (ghostRole.JobProto != null)
        {
            _roleSystem.MindAddJobRole(args.Mind, args.Mind, silent:false,ghostRole.JobProto);
        }

        ghostRole.Taken = true;
        UnregisterGhostRole((uid, ghostRole));
    }

    private void OnMindRemoved(EntityUid uid, GhostTakeoverAvailableComponent component, MindRemovedMessage args)
    {
        if (!TryComp(uid, out GhostRoleComponent? ghostRole))
            return;

        // Avoid re-registering it for duplicate entries and potential exceptions.
        if (!ghostRole.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
            return;

        ghostRole.Taken = false;
        RegisterGhostRole((uid, ghostRole));
    }

    public void Reset(RoundRestartCleanupEvent ev)
    {
        foreach (var session in _openUis.Keys)
        {
            CloseEui(session);
        }

        _openUis.Clear();
        _ghostRoles.Clear();
        _ghostRoleRaffles.Clear();
        _nextRoleIdentifier = 0;
    }

    private void OnPaused(EntityUid uid, GhostRoleComponent component, ref EntityPausedEvent args)
    {
        if (HasComp<ActorComponent>(uid))
            return;

        UpdateAllEui();
    }

    private void OnUnpaused(EntityUid uid, GhostRoleComponent component, ref EntityUnpausedEvent args)
    {
        if (HasComp<ActorComponent>(uid))
            return;

        UpdateAllEui();
    }

    private void OnMapInit(Entity<GhostRoleComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Probability < 1f && !_random.Prob(ent.Comp.Probability))
            RemCompDeferred<GhostRoleComponent>(ent);
    }

    private void OnRoleStartup(Entity<GhostRoleComponent> ent, ref ComponentStartup args)
    {
        RegisterGhostRole(ent);
    }

    private void OnRoleShutdown(Entity<GhostRoleComponent> role, ref ComponentShutdown args)
    {
        UnregisterGhostRole(role);
    }

    private void OnSpawnerTakeRole(EntityUid uid, GhostRoleMobSpawnerComponent component, ref TakeGhostRoleEvent args)
    {
        if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
            !CanTakeGhost(uid, ghostRole))
        {
            args.TookRole = false;
            return;
        }

        if (string.IsNullOrEmpty(component.Prototype))
            throw new NullReferenceException("Prototype string cannot be null or empty!");

        var mob = Spawn(component.Prototype, Transform(uid).Coordinates);
        _transform.AttachToGridOrMap(mob);

        var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, mob);
        RaiseLocalEvent(mob, spawnedEvent);

        if (ghostRole.MakeSentient)
            MakeSentientCommand.MakeSentient(mob, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

        EnsureComp<MindContainerComponent>(mob);

        GhostRoleInternalCreateMindAndTransfer(args.Player, uid, mob, ghostRole);

        if (++component.CurrentTakeovers < component.AvailableTakeovers)
        {
            args.TookRole = true;
            return;
        }

        ghostRole.Taken = true;

        if (component.DeleteOnSpawn)
            QueueDel(uid);

        args.TookRole = true;
    }

    private bool CanTakeGhost(EntityUid uid, GhostRoleComponent? component = null)
    {
        return Resolve(uid, ref component, false) &&
               !component.Taken &&
               !MetaData(uid).EntityPaused;
    }

    private void OnTakeoverTakeRole(EntityUid uid, GhostTakeoverAvailableComponent component, ref TakeGhostRoleEvent args)
    {
        if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
            !CanTakeGhost(uid, ghostRole))
        {
            args.TookRole = false;
            return;
        }

        ghostRole.Taken = true;

        var mind = EnsureComp<MindContainerComponent>(uid);

        if (mind.HasMind)
        {
            args.TookRole = false;
            return;
        }

        if (ghostRole.MakeSentient)
            MakeSentientCommand.MakeSentient(uid, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

        GhostRoleInternalCreateMindAndTransfer(args.Player, uid, uid, ghostRole);
        UnregisterGhostRole((uid, ghostRole));

        args.TookRole = true;
    }

    private void OnVerb(EntityUid uid, GhostRoleMobSpawnerComponent component, GetVerbsEvent<Verb> args)
    {
        var prototypes = component.SelectablePrototypes;
        if (prototypes.Count < 1)
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var verbs = new ValueList<Verb>();

        foreach (var prototypeID in prototypes)
        {
            if (_prototype.TryIndex<GhostRolePrototype>(prototypeID, out var prototype))
            {
                var verb = CreateVerb(uid, component, args.User, prototype);
                verbs.Add(verb);
            }
        }

        args.Verbs.UnionWith(verbs);
    }

    private Verb CreateVerb(EntityUid uid, GhostRoleMobSpawnerComponent component, EntityUid userUid, GhostRolePrototype prototype)
    {
        var verbText = Loc.GetString(prototype.Name);

        return new Verb()
        {
            Text = verbText,
            Disabled = component.Prototype == prototype.EntityPrototype,
            Category = VerbCategory.SelectType,
            Act = () => SetMode(uid, prototype, verbText, component, userUid)
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

    public void OnGhostRoleRadioMessage(Entity<GhostRoleMobSpawnerComponent> entity, ref GhostRoleRadioMessage args)
    {
        if (!_prototype.TryIndex(args.ProtoId, out var ghostRoleProto))
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
public sealed class GhostRoles : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _e = default!;

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
