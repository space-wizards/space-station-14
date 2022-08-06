using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles;

public record GhostRoleLotteryGroup(List<IPlayerSession> Requests, List<GhostRoleComponent> Components);

internal record GhostRolesEntry(List<GhostRoleComponent> Takeover, List<GhostRoleComponent> Lottery);

public sealed class GhostRolesChangedEventArgs
{
    public readonly IPlayerSession? UpdateSession = default!;

    public GhostRolesChangedEventArgs(IPlayerSession? session = null)
    {
        UpdateSession = session;
    }
}

public sealed class PlayerTakeoverCompleteEventArgs
{
    public readonly IPlayerSession Session;
    public readonly GhostRoleComponent Component;

    public PlayerTakeoverCompleteEventArgs(IPlayerSession session, GhostRoleComponent component)
    {
        Session = session;
        Component = component;
    }
}

[UsedImplicitly]
public sealed class GhostRoleManager
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly TimeSpan _lotteryElapseTime = TimeSpan.FromSeconds(30);
    public TimeSpan LotteryStartTime { get; private set; } = TimeSpan.Zero;
    public TimeSpan LotteryExpiresTime { get; private set; } = TimeSpan.Zero;

    private readonly List<GhostRoleComponent> _queuedGhostRoleComponents = new();
    private readonly Dictionary<string, GhostRolesEntry> _ghostRoles = new();

    private readonly Dictionary<IPlayerSession, List<string>> _playerLotteryRequests = new();

    public int AvailableRolesCount => _ghostRoles.Sum(ent => ent.Value.Lottery.Count + ent.Value.Takeover.Count);

    public string[] AvailableRoles => _ghostRoles.Keys.ToArray();

    private const string GhostRolePrefix = "GhostRole:";

    private uint _nextIdentifier;
    public uint NextIdentifier => unchecked(_nextIdentifier++);

    public event Action<GhostRolesChangedEventArgs>? OnGhostRolesChanged;
    public event Action<PlayerTakeoverCompleteEventArgs>? OnPlayerTakeoverComplete;


    public void QueueGhostRole(GhostRoleComponent component)
    {
        if (_queuedGhostRoleComponents.Contains(component))
            return;

        var identifier = GhostRolePrefix + component.RoleName;

        if (_ghostRoles.TryGetValue(identifier, out var roles) &&
            (roles.Lottery.Contains(component) || roles.Takeover.Contains(component)))
            return;

        _queuedGhostRoleComponents.Add(component);
    }

    private void InternalAddGhostRole(GhostRoleComponent component)
    {
        if (!component.Running)
            return; //

        var identifier = GhostRolePrefix + component.RoleName;

        if (!_ghostRoles.TryGetValue(identifier, out var roles))
            _ghostRoles[identifier] = roles = new GhostRolesEntry(new List<GhostRoleComponent>(), new List<GhostRoleComponent>());

        var addTo = component.RoleUseLottery ? roles.Lottery : roles.Takeover;

        if (!addTo.Contains(component))
            addTo.Add(component);

        SendGhostRolesChangedEvent();
    }

    public void RemoveGhostRole(GhostRoleComponent component)
    {
        _queuedGhostRoleComponents.Remove(component);

        var identifier = GhostRolePrefix + component.RoleName;

        if (!_ghostRoles.TryGetValue(identifier, out var roles))
            return;

        var removed = false;
        removed |= roles.Lottery.Remove(component);
        removed |= roles.Takeover.Remove(component);

        if(removed)
            SendGhostRolesChangedEvent();
    }

    public void AddPlayerRequest(IPlayerSession player, string identifier)
    {
        if (!_playerLotteryRequests.TryGetValue(player, out var requests))
            _playerLotteryRequests[player] = requests = new List<string>();

        if(!requests.Contains(identifier))
            requests.Add(identifier);

        SendGhostRolesChangedEvent(player);
    }

    public void RemovePlayerRequest(IPlayerSession player, string identifier)
    {
        if (_playerLotteryRequests.TryGetValue(player, out var requests) && requests.Contains(identifier))
            requests.Remove(identifier);

        SendGhostRolesChangedEvent(player);
    }

    public void ClearPlayerRequests(IPlayerSession player)
    {
        _playerLotteryRequests.Remove(player);
        SendGhostRolesChangedEvent(player);
    }

    private bool TryPopTakeoverGhostComponent(string roleIdentifier, [MaybeNullWhen(false)] out GhostRoleComponent component)
    {
        component = null;

        if (!_ghostRoles.TryGetValue(roleIdentifier, out var roles))
            return false;

        if (roles.Takeover.Count == 0)
            return false;

        component = roles.Takeover.Pop();
        SendGhostRolesChangedEvent();
        return true;
    }

    public bool TryGetFirstGhostRoleComponent(string roleIdentifier, [MaybeNullWhen(false)] out GhostRoleComponent component)
    {
        component = null;
        if (!_ghostRoles.TryGetValue(roleIdentifier, out var roles))
            return false;

        if (roles.Lottery.Count > 0)
        {
            component = roles.Lottery.First();
            return true;
        }

        if (roles.Takeover.Count <= 0)
            return false;

        component = roles.Takeover.First();
        return true;
    }

    public GhostRoleComponent? GetNextGhostRoleComponentOrNull(string roleIdentifier, uint identifier)
    {
        if (!_ghostRoles.TryGetValue(roleIdentifier, out var roles))
            return null;


        var allGhostRoles = roles.Lottery.Concat(roles.Takeover).ToList();

        if (allGhostRoles.Count <= 1)
            return allGhostRoles.FirstOrDefault();

        GhostRoleComponent? component = null;
        GhostRoleComponent? prev = null;
        foreach (var role in allGhostRoles)
        {
            if (role.Identifier == identifier)
            {
                prev = role;
            }
            else if (prev?.Identifier == identifier)
            {
                component = role;
                break;
            }
        }

        return component ?? allGhostRoles.First();
    }

    public void TakeoverImmediate(IPlayerSession player, string identifier)
    {
        if (!TryPopTakeoverGhostComponent(identifier, out var role))
            return;

        if (!role.Take(player))
            return; // Currently only fails if the role is already taken.

        ClearPlayerRequests(player);
        SendPlayerTakeoverCompleteEvent(player, role);
    }

    public GhostRoleInfo[] GetGhostRolesInfo(IPlayerSession session)
    {
        var roles = new List<GhostRoleInfo>(_ghostRoles.Count);

        foreach (var (id, entry) in _ghostRoles)
        {
            if(entry.Lottery.Count == 0 && entry.Takeover.Count == 0)
                continue;

            var comp = entry.Lottery.Count > 0 ? entry.Lottery.First() : entry.Takeover.First();

            var role = new GhostRoleInfo()
            {
                Identifier = id,
                Name = comp.RoleName,
                Description = comp.RoleDescription,
                Rules = comp.RoleRules,
                IsRequested = _playerLotteryRequests.GetValueOrDefault(session)?.Contains(id) ?? false,
                AvailableLotteryRoleCount = entry.Lottery.Count,
                AvailableImmediateRoleCount = entry.Takeover.Count,
            };

            roles.Add(role);
        }

        return roles.ToArray();
    }

    private Dictionary<string, GhostRoleLotteryGroup> BuildLotteryGroups()
    {
        // Run the lotteries on each ghost role grouping.
        var lottery = new Dictionary<string, GhostRoleLotteryGroup>();
        foreach (var (roleIdentifier, components) in _ghostRoles)
        {
            var group = new GhostRoleLotteryGroup(new List<IPlayerSession>(), new List<GhostRoleComponent>(components.Lottery));
            lottery[roleIdentifier] = group;
            components.Lottery.Clear();
        }

        foreach (var (player, requests) in _playerLotteryRequests)
        {
            foreach (var request in requests)
            {
                if(lottery.TryGetValue(request, out var group) && !group.Requests.Contains(player))
                    group.Requests.Add(player);
            }
        }

        foreach (var (_, group) in lottery)
        {
            _random.Shuffle(group.Requests);
        }

        _playerLotteryRequests.Clear();
        return lottery;
    }

    public void Update()
    {
        if (_gameTiming.CurTime < LotteryExpiresTime)
            return;

        BuildLotteryGroups();

        var successfulPlayers = new HashSet<IPlayerSession>();
        var pendingLotteries = BuildLotteryGroups();
        foreach (var (_, group) in pendingLotteries)
        {
            ProcessLottery(group, successfulPlayers);
        }

        // Add pending components.
        foreach (var component in _queuedGhostRoleComponents)
        {
            InternalAddGhostRole(component);
        }
        _queuedGhostRoleComponents.Clear();

        LotteryStartTime = _gameTiming.CurTime;
        LotteryExpiresTime = LotteryStartTime + _lotteryElapseTime;
        SendGhostRolesChangedEvent();
    }

    private void ProcessLottery(GhostRoleLotteryGroup entry, HashSet<IPlayerSession> successfulPlayers)
    {
        var playerCount = entry.Requests.Count;
        var componentCount = entry.Components.Count;

        if (playerCount == 0 || componentCount == 0)
            return;

        var sessionIdx = 0;
        var componentIdx = 0;

        while (sessionIdx < playerCount && componentIdx < componentCount)
        {
            var session = entry.Requests[sessionIdx];
            var component = entry.Components[sessionIdx];

            if (session.Status != SessionStatus.InGame || successfulPlayers.Contains(session))
            {
                sessionIdx++;
                continue;
            }

            if (!component.Take(session))
            {
                componentIdx++;
                continue;
            }


            // A single GhostRoleMobSpawnerComponent can spawn multiple entities. Check it is completely used up.
            if (component.Taken)
                componentIdx++;

            sessionIdx++;
            componentIdx++;

            successfulPlayers.Add(session);
            SendPlayerTakeoverCompleteEvent(session, component);
        }

        // Re-add remaining components.
        while (componentIdx < componentCount)
        {
            InternalAddGhostRole(entry.Components[componentIdx]);
            componentIdx++;
        }
    }

    private void SendGhostRolesChangedEvent(IPlayerSession? session = null)
    {
        OnGhostRolesChanged?.Invoke(new GhostRolesChangedEventArgs(session));
    }

    private void SendPlayerTakeoverCompleteEvent(IPlayerSession session, GhostRoleComponent component)
    {
        OnPlayerTakeoverComplete?.Invoke(new PlayerTakeoverCompleteEventArgs(session, component));
    }
}
