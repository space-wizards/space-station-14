using System.Linq;
using Content.Server.EUI;
using Content.Server.Ghost.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Ghost.Roles.UI;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Ghost.Roles;

/// <summary>
///     Stores the requests by identifier for ghost roles and ghost role groups.
/// </summary>
internal sealed class PlayerLotteryData
{
    public readonly HashSet<string> GhostRoles = new();
    public readonly HashSet<uint> GhostRoleGroups = new();
}

/// <summary>
///     Handles the logic for the ghost roles EUI and manages lotteries for <see cref="GhostRoleSystem"/> and
///     <see cref="GhostRoleGroupSystem"/>.
///     <para/>
///
///     Lotteries share a single expiration timer, and will all be run simultaneously. New lotteries are added
///     after the current lottery expires.
/// </summary>
[UsedImplicitly]
public sealed class GhostRoleLotterySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    /// <summary>
    ///     Open EUIs per-player.
    /// </summary>
    private readonly Dictionary<IPlayerSession, GhostRolesEui> _openUis = new();

    /// <summary>
    ///     Lottery requests per-player.
    /// </summary>
    private readonly Dictionary<IPlayerSession, PlayerLotteryData> _playerLotteryData = new ();

    /// <summary>
    ///     Ghost roles put up for the current lottery.
    /// </summary>
    private readonly HashSet<string> _ghostRoleComponentLotteries = new();

    /// <summary>
    ///     Ghost role groups put up for the current lottery.
    /// </summary>
    private readonly HashSet<uint> _roleGroupLotteries = new();

    private bool _needsUpdateGhostRoles = true;
    private bool _needsUpdateGhostRoleCount = true;

    /// <summary>
    ///     Time the lottery started. Uses synced time.
    /// </summary>
    public TimeSpan LotteryStartTime { get; private set; } = TimeSpan.Zero;

    /// <summary>
    ///     Time the lotteries will expire at. Uses synced time.
    /// </summary>
    public TimeSpan LotteryExpiresTime { get; private set; } = TimeSpan.Zero;

    /// <summary>
    ///     Cached available role count, representing the total number of available spawns. Be aware that certain
    ///     items (see <see cref="GhostRoleMobSpawnerComponent"/>) can be worth multiple spawns.
    /// </summary>
    public int AvailableRolesCount { get; private set; }

    /// <summary>
    ///     List of unique available roles. Used by the UI so it can highlight the ghost roles button
    ///     when a brand-new role is added.
    ///     TODO: Reimplement or rework this.
    /// </summary>
    public string[] AvailableRoles => new string[] { };


    private uint _nextIdentifier = 1;
    /// <summary>
    ///     Identifier used for groups of ghost role components and role groups.
    /// </summary>
    public uint NextIdentifier => unchecked(_nextIdentifier++);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);

        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        foreach (var session in _openUis.Keys)
        {
            CloseEui(session);
        }

        _openUis.Clear();
        _playerLotteryData.Clear();
        _ghostRoleComponentLotteries.Clear();
        _roleGroupLotteries.Clear();

        LotteryStartTime = TimeSpan.Zero;
        LotteryExpiresTime = TimeSpan.Zero;
    }

    private void PlayerStatusChanged(object? _, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame)
            return;

        var response = new GhostUpdateGhostRoleCountEvent(AvailableRolesCount, AvailableRoles);
        RaiseNetworkEvent(response, args.Session.ConnectedClient);
    }

    #region UI
    public void OpenEui(IPlayerSession session)
    {
        if (session.AttachedEntity is not {Valid: true} attached ||
            !_entityManager.HasComponent<GhostComponent>(attached))
            return;

        if(_openUis.ContainsKey(session))
            CloseEui(session);

        var eui = _openUis[session] = new GhostRolesEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    private void UpdatePlayerEui(IPlayerSession session)
    {
        if (!_openUis.TryGetValue(session, out var ui))
            return;

        ui.StateDirty();
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

    public void CloseEui(IPlayerSession session)
    {
        if (!_openUis.ContainsKey(session))
            return;

        _openUis.Remove(session, out var eui);
        eui?.Close();
    }

    private void UpdateUi()
    {
        if (_needsUpdateGhostRoles)
        {
            _needsUpdateGhostRoles = false;
            UpdateAllEui();
        }

        if (!_needsUpdateGhostRoleCount)
            return;

        _needsUpdateGhostRoleCount = false;
        var ghostRoleSystem = Get<GhostRoleSystem>();
        var ghostRoleGroupSystem = Get<GhostRoleGroupSystem>();

        AvailableRolesCount = 0;
        AvailableRolesCount += ghostRoleSystem.GetAvailableCount();
        AvailableRolesCount += ghostRoleGroupSystem.GetAvailableCount();

        var response = new GhostUpdateGhostRoleCountEvent(AvailableRolesCount, AvailableRoles);
        foreach (var player in _playerManager.Sessions)
        {
            RaiseNetworkEvent(response, player.ConnectedClient);
        }
    }

    /// <summary>
    ///     Retrieves the ghost roles requested by the player.
    /// </summary>
    /// <param name="player">The player to retrieve requests for.</param>
    /// <returns>Identifiers of ghost roles requested by the player.</returns>
    public string[] GetPlayerRequestedGhostRoles(IPlayerSession player)
    {
        var requested = new List<string>();
        if (!_playerLotteryData.TryGetValue(player, out var data))
            return requested.ToArray();

        requested.AddRange(data.GhostRoles);

        return requested.ToArray();
    }

    /// <summary>
    ///     Retrieves the ghost role groups requested by the player.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public uint[] GetPlayerRequestedRoleGroups(IPlayerSession player)
    {
        var requested = new List<uint>();
        if (!_playerLotteryData.TryGetValue(player, out var data))
            return requested.ToArray();

        requested.AddRange(data.GhostRoleGroups);

        return requested.ToArray();
    }
    #endregion

    private void OnPlayerAttached(PlayerAttachedEvent message)
    {
        // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
        if (!_openUis.ContainsKey(message.Player))
            return;
        if (EntityManager.HasComponent<GhostComponent>(message.Entity))
            return;

        CloseEui(message.Player);
        ClearPlayerLotteryRequests(message.Player);
    }

    /// <summary>
    ///     Removes every type of request for a single player.
    /// </summary>
    /// <param name="player">The player to clear all requests for.</param>
    public void ClearPlayerLotteryRequests(IPlayerSession player)
    {
        if (!_playerLotteryData.TryGetValue(player, out var data))
            return;

        data.GhostRoles.Clear();
        data.GhostRoleGroups.Clear();
        UpdatePlayerEui(player);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTiming.CurTime < LotteryExpiresTime)
        {
            UpdateUi();
            return;
        }

        _needsUpdateGhostRoles = true;
        _needsUpdateGhostRoleCount = true;

        var successfulPlayers = new HashSet<IPlayerSession>();
        ProcessRoleGroupLottery(successfulPlayers);
        ProcessGhostRoleLottery(successfulPlayers);

        // Prepare to start the next lottery.
        var elapseTime = TimeSpan.FromSeconds(_cfgManager.GetCVar<float>(CCVars.GhostRoleLotteryTime));
        LotteryStartTime = _gameTiming.CurTime;
        LotteryExpiresTime = LotteryStartTime + elapseTime;

        var ghostRoleSystem = Get<GhostRoleSystem>();
        var ghostRoleGroupSystem = Get<GhostRoleGroupSystem>();

        // TODO: OnNextLotteryStartingEvent?
        ghostRoleSystem.OnNextLotteryStarting();
        ghostRoleGroupSystem.OnNextLotteryStarting();

        var ghostRoleComponents = ghostRoleSystem.GetAllAvailableLotteryItems();
        var ghostRoleGroups = ghostRoleGroupSystem.GetAllAvailableLotteryItems();

        // Update ghost role group lotteries.
        var removedRoleGroups = _roleGroupLotteries.Where(rg => !ghostRoleGroups.Contains(rg)).ToArray();
        _roleGroupLotteries.Clear();
        _roleGroupLotteries.UnionWith(ghostRoleGroups);

        // Update ghost role lotteries.
        var removedRoles = _ghostRoleComponentLotteries.Where(r => !ghostRoleComponents.Contains(r)).ToArray();
        _ghostRoleComponentLotteries.Clear();
        _ghostRoleComponentLotteries.UnionWith(ghostRoleComponents);

        foreach (var (_, data) in _playerLotteryData)
        {
            data.GhostRoles.ExceptWith(removedRoles);
            data.GhostRoleGroups.ExceptWith(removedRoleGroups);
        }

        UpdateUi();
    }

    #region Ghost Role
    /// <summary>
    ///     Adds the player request for a lottery-enabled ghost role.
    /// </summary>
    /// <param name="player">The player to add the lottery request for.</param>
    /// <param name="identifier">Identifier of the group of ghost roles.</param>
    public void GhostRoleAddPlayerLotteryRequest(IPlayerSession player, string identifier)
    {
        if (!_playerLotteryData.TryGetValue(player, out var data))
            _playerLotteryData[player] = data = new PlayerLotteryData();

        if (!data.GhostRoles.Add(identifier))
            return;

        UpdatePlayerEui(player);
    }

    /// <summary>
    ///     Removes the player request for a lottery-enabled ghost role.
    /// </summary>
    /// <param name="player">The player to remove the lottery request for.</param>
    /// <param name="identifier">Identifier of the group of ghost roles.</param>
    public void GhostRoleRemovePlayerLotteryRequest(IPlayerSession player, string identifier)
    {
        if (!_playerLotteryData.TryGetValue(player, out var data))
            return;

        if (data.GhostRoles.Remove(identifier))
            UpdatePlayerEui(player);
    }

    /// <summary>
    ///     Run's the lottery for all lottery-able ghost roles.
    /// </summary>
    /// <param name="successfulPlayers">Player's that have already been given a ghost role. These should be skipped.</param>
    private void ProcessGhostRoleLottery(ISet<IPlayerSession> successfulPlayers)
    {
        var ghostRoleSystem = Get<GhostRoleSystem>();
        var sessions = new List<IPlayerSession>(_playerLotteryData.Count);
        var clearRoles = new HashSet<string>();

        foreach (var ghostRoleIdentifier in _ghostRoleComponentLotteries)
        {
            sessions.Clear();
            foreach (var (playerSession, data) in _playerLotteryData)
            {
                if(data.GhostRoles.Contains(ghostRoleIdentifier))
                    sessions.Add(playerSession);
            }

            if (sessions.Count == 0)
                continue;

            _random.Shuffle(sessions);
            ghostRoleSystem.OnLotteryResults(ghostRoleIdentifier, sessions, out var newSuccessfulSessions, out var rolesTaken);

            foreach (var takenPlayer in newSuccessfulSessions)
            {
                successfulPlayers.Add(takenPlayer);
                ClearPlayerLotteryRequests(takenPlayer);
                CloseEui(takenPlayer);
            }

            if(rolesTaken)
                clearRoles.Add(ghostRoleIdentifier);
        }

        foreach (var (_, data) in _playerLotteryData)
        {
            data.GhostRoles.ExceptWith(clearRoles);
        }
    }
    #endregion

    #region Ghost Role Group
    /// <summary>
    ///     Adds the player request for a ghost role group.
    /// </summary>
    /// <param name="player">The player to add the request for.</param>
    /// <param name="identifier">The identifier of the role group.</param>
    public void GhostRoleGroupAddPlayerLotteryRequest(IPlayerSession player, uint identifier)
    {
        if (!_playerLotteryData.TryGetValue(player, out var data))
            _playerLotteryData[player] = data = new PlayerLotteryData();

        if (data.GhostRoleGroups.Add(identifier))
            UpdatePlayerEui(player);
    }

    /// <summary>
    ///     Removes the player request for a ghost role group.
    /// </summary>
    /// <param name="player">The player to remove the request for.</param>
    /// <param name="identifier">The identifier of the role group.</param>
    public void GhostRoleGroupRemovePlayerLotteryRequest(IPlayerSession player, uint identifier)
    {
        if (!_playerLotteryData.TryGetValue(player, out var data))
            return;

        if (data.GhostRoleGroups.Remove(identifier))
            UpdatePlayerEui(player);
    }

    /// <summary>
    ///     Run's the lottery for all released ghost role groups.
    /// </summary>
    /// <param name="successfulPlayers">Player that have already been given a role, these should be skipped.</param>
    private void ProcessRoleGroupLottery(ISet<IPlayerSession> successfulPlayers)
    {
        var ghostRoleGroupSystem = Get<GhostRoleGroupSystem>();
        var sessions = new List<IPlayerSession>(_playerLotteryData.Count);
        var clearRoleGroups = new HashSet<uint>();

         foreach (var roleGroupIdentifier in _roleGroupLotteries)
         {
             foreach (var (session, data) in _playerLotteryData)
             {
                 if (data.GhostRoleGroups.Contains(roleGroupIdentifier))
                     sessions.Add(session);
             }

             var playerCount = sessions.Count;
             if (playerCount == 0)
                 continue;

             _random.Shuffle(sessions);
             ghostRoleGroupSystem.OnLotteryResults(roleGroupIdentifier, sessions, out var newSuccessfulSessions, out var roleGroupTaken);

             foreach (var takenPlayer in newSuccessfulSessions)
             {
                 successfulPlayers.Add(takenPlayer);
                 ClearPlayerLotteryRequests(takenPlayer);
                 CloseEui(takenPlayer);
             }

             if(roleGroupTaken)
                 clearRoleGroups.Add(roleGroupIdentifier);
         }

         foreach (var (_, data) in _playerLotteryData)
         {
             data.GhostRoleGroups.ExceptWith(clearRoleGroups);
         }
    }
    #endregion
}

[AnyCommand]
public sealed class GhostRoles : IConsoleCommand
{
    public string Command => "ghostroles";
    public string Description => "Opens the ghost role request window.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if(shell.Player is IPlayerSession playerSession)
            EntitySystem.Get<GhostRoleLotterySystem>().OpenEui(playerSession);
        else
            shell.WriteLine("You can only open the ghost roles UI on a client.");
    }
}

