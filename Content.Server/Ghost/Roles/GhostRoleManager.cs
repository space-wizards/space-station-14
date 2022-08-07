using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Players;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles;

public enum GhostRoleGroupStatus : byte
{
    Editing = 0,
    Releasing = 1,
    Released = 2,
}

internal record GhostRolesEntry(string RoleIdentifier, List<uint> Takeover, List<uint> Lottery, HashSet<IPlayerSession> Requests);

internal sealed class RoleGroupEntry
{
    public uint Identifier { get; init; }
    public IPlayerSession Owner { get; init; } = default!;
    public string RoleName { get; init; } = default!;
    public string RoleDescription { get; init; } = default!;

    public bool IsActive;

    public GhostRoleGroupStatus Status = GhostRoleGroupStatus.Editing;

    public readonly List<EntityUid> Entities = new();

    public readonly HashSet<IPlayerSession> Requests = new();
}

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
    public readonly EntityUid Entity;
    public readonly GhostRoleComponent? Component;

    public readonly string RoleName;

    public PlayerTakeoverCompleteEventArgs(IPlayerSession session, GhostRoleComponent component)
    {
        Session = session;
        Entity = component.Owner;
        Component = component;
        RoleName = component.RoleName;
    }

    public PlayerTakeoverCompleteEventArgs(IPlayerSession session, string roleName, EntityUid entity)
    {
        Session = session;
        Entity = entity;
        Component = null;
        RoleName = roleName;
    }
}

[UsedImplicitly]
public sealed class GhostRoleManager
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    public TimeSpan LotteryStartTime { get; private set; } = TimeSpan.Zero;
    public TimeSpan LotteryExpiresTime { get; private set; } = TimeSpan.Zero;

    private readonly List<GhostRoleComponent> _queuedGhostRoleComponents = new();
    private readonly Dictionary<uint, GhostRoleComponent> _ghostRoleComponents = new();

    private readonly Dictionary<string, GhostRolesEntry> _ghostRoleEntries = new();
    private readonly Dictionary<uint, RoleGroupEntry> _roleGroupEntries = new();



    public int AvailableRolesCount => _ghostRoleEntries.Sum(ent => ent.Value.Lottery.Count + ent.Value.Takeover.Count);

    public string[] AvailableRoles => _ghostRoleEntries.Keys.ToArray();

    private const string GhostRolePrefix = "GhostRole:";
    private const string GhostRoleGroupPrefix = "GhostRoleGroupPrefix:";

    private uint _nextIdentifier;
    public uint NextIdentifier => unchecked(_nextIdentifier++);

    public event Action<GhostRolesChangedEventArgs>? OnGhostRolesChanged;
    public event Action<PlayerTakeoverCompleteEventArgs>? OnPlayerTakeoverComplete;


    public void QueueGhostRole(GhostRoleComponent component)
    {
        if (_queuedGhostRoleComponents.Contains(component))
            return;

        var identifier = GhostRolePrefix + component.RoleName;

        if (_ghostRoleEntries.TryGetValue(identifier, out var roles) &&
            (roles.Lottery.Contains(component.Identifier) || roles.Takeover.Contains(component.Identifier)))
            return;

        _queuedGhostRoleComponents.Add(component);
    }

    private void InternalAddGhostRole(GhostRoleComponent component)
    {
        if (!component.Running)
            return; //

        var roleIdentifier = GhostRolePrefix + component.RoleName;

        if (!_ghostRoleEntries.TryGetValue(roleIdentifier, out var roles))
            _ghostRoleEntries[roleIdentifier] = roles = new GhostRolesEntry(roleIdentifier, new List<uint>(), new List<uint>(), new HashSet<IPlayerSession>());

        var addTo = component.RoleUseLottery ? roles.Lottery : roles.Takeover;

        if (!addTo.Contains(component.Identifier))
            addTo.Add(component.Identifier);

        _ghostRoleComponents.Add(component.Identifier, component);
        SendGhostRolesChangedEvent();
    }

    public void RemoveGhostRole(GhostRoleComponent component)
    {
        _queuedGhostRoleComponents.Remove(component);

        var roleIdentifier = GhostRolePrefix + component.RoleName;

        if (!_ghostRoleEntries.TryGetValue(roleIdentifier, out var roles))
            return;

        var removed = false;
        removed |= roles.Lottery.Remove(component.Identifier);
        removed |= roles.Takeover.Remove(component.Identifier);

        if (!removed)
            return;

        SendGhostRolesChangedEvent();
        _ghostRoleComponents.Remove(component.Identifier);
    }

    public void AddGhostRoleLotteryRequest(IPlayerSession player, string identifier)
    {
        if (!_ghostRoleEntries.TryGetValue(identifier, out var entry))
            return;

        if (entry.Requests.Contains(player))
            return;

        entry.Requests.Add(player);
        SendGhostRolesChangedEvent(player);
    }

    public void AddRoleGroupLotteryRequest(IPlayerSession player, uint identifier)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return;

        if (entry.Requests.Contains(player))
            return;

        entry.Requests.Add(player);
        SendGhostRolesChangedEvent(player);
    }

    public void RemoveGhostRoleLotteryRequest(IPlayerSession player, string identifier)
    {
        if (!_ghostRoleEntries.TryGetValue(identifier, out var entry))
            return;

        if (entry.Requests.Remove(player))
            SendGhostRolesChangedEvent(player);
    }

    public void RemoveRoleGroupLotteryRequest(IPlayerSession player, uint identifier)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return;

        if(entry.Requests.Remove(player))
            SendGhostRolesChangedEvent(player);
    }

    public void ClearPlayerRequests(IPlayerSession player)
    {
        foreach(var (_,entry) in _ghostRoleEntries)
        {
            entry.Requests.Remove(player);
        }

        foreach (var (_, entry) in _roleGroupEntries)
        {
            entry.Requests.Remove(player);
        }

        SendGhostRolesChangedEvent(player);
    }

    public void StartGhostRoleGroup(IPlayerSession session)
    {
        if (!_adminManager.IsAdmin(session))
            return;

        if (_roleGroupEntries.FirstOrNull(kv => kv.Value.Owner == session) != null)
            return;

        var identifier = NextIdentifier;
        var entry = new RoleGroupEntry()
        {
            Owner = session,
            Identifier = identifier,
            RoleName = $"Role Group [{identifier}]",
            RoleDescription = "Custom role group",
            IsActive = true,
        };

        // TODO: Multiple role group entries per player.

        _roleGroupEntries.Add(identifier, entry);
    }

    public void ReleaseGhostRoleGroup(IPlayerSession session, uint identifier)
    {
        if (!_adminManager.IsAdmin(session))
            return;

        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return;

        if (entry.Owner != session)
            return;

        entry.Status = GhostRoleGroupStatus.Releasing;
    }

    public uint? GetActiveGhostRoleGroupOrNull(IPlayerSession session)
    {
        if (!_adminManager.IsAdmin(session))
            return null;

        if (!_roleGroupEntries.TryFirstOrNull(kv => kv.Value.Owner == session && kv.Value.IsActive, out var entry))
            return null;

        return entry.Value.Value.Identifier;
    }

    private bool InternalAttachToGhostRoleGroup(IPlayerSession session, uint identifier, EntityUid entity)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return false;

        if (entry.Owner != session || entry.Status != GhostRoleGroupStatus.Editing)
            return false;

        if (entry.Entities.Contains(entity))
            return true;

        entry.Entities.Add(entity);
        return true;
    }

    public bool TryAttachToActiveGhostRoleGroup(IPlayerSession session, GhostRoleComponent component)
    {
        if (!_adminManager.IsAdmin(session))
            return false;

        if (!_roleGroupEntries.TryFirstOrNull(e => e.Value.Owner == session && e.Value.IsActive, out var entry))
            return false;

        var group = entry.Value.Value;

        var result = InternalAttachToGhostRoleGroup(session, group.Identifier, component.Owner);
        if (result)
            _queuedGhostRoleComponents.Remove(component);

        return result;
    }

    private bool TryPopTakeoverGhostComponent(string roleIdentifier, [MaybeNullWhen(false)] out GhostRoleComponent component)
    {
        var idx = 0u;
        component = null;

        if (!_ghostRoleEntries.TryGetValue(roleIdentifier, out var roles))
            return false;

        if (roles.Takeover.Count == 0)
            return false;

        idx = roles.Takeover.Pop();
        if (!_ghostRoleComponents.TryGetValue(idx, out component))
            return false;

        _ghostRoleComponents.Remove(idx);
        SendGhostRolesChangedEvent();
        return true;
    }

    public bool TryGetFirstGhostRoleComponent(string roleIdentifier, [MaybeNullWhen(false)] out GhostRoleComponent component)
    {
        component = null;
        if (!_ghostRoleEntries.TryGetValue(roleIdentifier, out var roles))
            return false;

        if (roles.Lottery.Count == 0 && roles.Takeover.Count == 0)
            return false;

        if (roles.Lottery.Count > 0)
        {
            var idx = roles.Lottery.First();
            return _ghostRoleComponents.TryGetValue(idx, out component);
        }
        else
        {
            var idx = roles.Takeover.First();
            return _ghostRoleComponents.TryGetValue(idx, out component);
        }
    }

    public GhostRoleComponent? GetNextGhostRoleComponentOrNull(string roleIdentifier, uint identifier)
    {
        if (!_ghostRoleEntries.TryGetValue(roleIdentifier, out var roles))
            return null;

        var allGhostRoles = roles.Lottery.Concat(roles.Takeover).ToList();

        if (allGhostRoles.Count <= 1)
        {
            var idx = allGhostRoles.FirstOrNull();
            return idx != null ? _ghostRoleComponents.GetValueOrDefault(idx.Value) : null;
        }

        GhostRoleComponent? component = null;
        GhostRoleComponent? prev = null;
        foreach (var roleIdx in allGhostRoles)
        {
            if(!_ghostRoleComponents.TryGetValue(roleIdx, out var role))
                continue;

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

        return component ?? _ghostRoleComponents.GetValueOrDefault(allGhostRoles.First());
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

    public GhostRoleGroupInfo[] GetGhostRoleGroupsInfo(IPlayerSession session)
    {
        var isAdmin = _adminManager.IsAdmin(session);

        var groups = new List<GhostRoleGroupInfo>(_roleGroupEntries.Count);
        foreach (var (_, group) in _roleGroupEntries)
        {
            if (group.Status != GhostRoleGroupStatus.Released && !isAdmin)
                continue;

            groups.Add(new GhostRoleGroupInfo()
            {
                GroupIdentifier = group.Identifier,
                Identifier = $"{GhostRoleGroupPrefix}{group.Identifier}",
                AvailableCount = group.Entities.Count,
                Name = group.RoleName,
                Description = group.RoleDescription,
                Rules = "Test",
                Status = group.Status.ToString(),
                IsRequested = group.Requests.Contains(session)
            });
        }

        return groups.ToArray();
    }

    public GhostRoleInfo[] GetGhostRolesInfo(IPlayerSession session)
    {
        var roles = new List<GhostRoleInfo>(_ghostRoleEntries.Count);

        foreach (var (id, entry) in _ghostRoleEntries)
        {
            if(entry.Lottery.Count == 0 && entry.Takeover.Count == 0)
                continue;

            var compIdx = entry.Lottery.Count > 0 ? entry.Lottery.First() : entry.Takeover.First();
            if (!_ghostRoleComponents.TryGetValue(compIdx, out var comp))
                continue;

            var role = new GhostRoleInfo()
            {
                Identifier = id,
                Name = comp.RoleName,
                Description = comp.RoleDescription,
                Rules = comp.RoleRules,
                IsRequested = entry.Requests.Contains(session),
                AvailableLotteryRoleCount = entry.Lottery.Count,
                AvailableImmediateRoleCount = entry.Takeover.Count,
            };

            roles.Add(role);
        }

        return roles.ToArray();
    }

    public void Update()
    {
        if (_gameTiming.CurTime < LotteryExpiresTime)
            return;

        var successfulPlayers = new HashSet<IPlayerSession>();
        ProcessRoleGroupLottery(successfulPlayers);
        ProcessGhostRoleLottery(successfulPlayers);

        // Transition ghost role groups.
        foreach (var (_, group) in _roleGroupEntries)
        {
            if (group.Status == GhostRoleGroupStatus.Releasing)
                group.Status = GhostRoleGroupStatus.Released;
        }

        // Add pending components.
        foreach (var component in _queuedGhostRoleComponents)
        {
            InternalAddGhostRole(component);
        }
        _queuedGhostRoleComponents.Clear();

        var elapseTime = TimeSpan.FromSeconds(_cfgManager.GetCVar<float>(CCVars.GhostRoleLotteryTime));

        LotteryStartTime = _gameTiming.CurTime;
        LotteryExpiresTime = LotteryStartTime + elapseTime;
        SendGhostRolesChangedEvent();
    }

    private void ProcessGhostRoleLottery(ISet<IPlayerSession> successfulPlayers)
    {
        foreach (var (_, entry) in _ghostRoleEntries)
        {
            var playerCount = entry.Requests.Count;
            var lotteryCount = 0;

            foreach (var idx in entry.Lottery)
            {
                if (!_ghostRoleComponents.TryGetValue(idx, out var comp))
                    continue;

                if (comp is GhostRoleMobSpawnerComponent spawnerComponent)
                    lotteryCount += spawnerComponent.AvailableTakeovers;
                else
                    lotteryCount += 1;
            }

            if (playerCount == 0)
                continue;

            if (lotteryCount == 0)
            {
                entry.Requests.Clear();
                continue;
            }

            var sessionIdx = 0;
            var lotteryIdx = 0;

            var lottery = entry.Lottery.ShallowClone();
            entry.Lottery.Clear();

            var shuffledRequests = entry.Requests.ToList();
            _random.Shuffle(shuffledRequests);
            entry.Requests.Clear();

            while (sessionIdx < playerCount && lotteryIdx < lotteryCount)
            {
                var session = shuffledRequests[sessionIdx];
                var compIdx = lottery[lotteryIdx];

                if (session.Status != SessionStatus.InGame || successfulPlayers.Contains(session))
                {
                    sessionIdx++;
                    continue;
                }

                if (!_ghostRoleComponents.TryGetValue(compIdx, out var component) || !component.Take(session))
                {
                    lotteryIdx++;
                    continue;
                }

                // A single GhostRoleMobSpawnerComponent can spawn multiple entities. Check it is completely used up.
                if (component.Taken)
                    lotteryIdx++;

                sessionIdx++;

                successfulPlayers.Add(session);
                SendPlayerTakeoverCompleteEvent(session, component);
            }

            // Re-add remaining components.
            while (lotteryIdx < lotteryCount)
            {
                var compIdx = lottery[lotteryIdx];
                if(_ghostRoleComponents.TryGetValue(compIdx, out var component) && !component.Taken)
                    entry.Lottery.Add(component.Identifier);

                lotteryIdx++;
            }
        }
    }

    private void ProcessRoleGroupLottery(ISet<IPlayerSession> successfulPlayers)
    {
        foreach (var (_, entry) in _roleGroupEntries)
        {
            var playerCount = entry.Requests.Count;
            var lotteryCount = entry.Entities.Count;

            if (playerCount == 0)
                return;

            if (lotteryCount == 0)
            {
                entry.Requests.Clear();
                return;
            }

            var sessionIdx = 0;
            var lotteryIdx = 0;

            var lottery = entry.Entities.ShallowClone();
            entry.Entities.Clear();

            var shuffledRequests = entry.Requests.ToList();
            _random.Shuffle(shuffledRequests);
            entry.Requests.Clear();

            while (sessionIdx < playerCount && lotteryIdx < lotteryCount)
            {
                var session = shuffledRequests[sessionIdx];
                var entityUid = lottery[lotteryIdx];

                if (session.Status != SessionStatus.InGame || successfulPlayers.Contains(session))
                {
                    sessionIdx++;
                    continue;
                }

                var contentData = session.ContentData();

                DebugTools.AssertNotNull(contentData);

                var newMind = new Mind.Mind(session.UserId)
                {
                    CharacterName = _entityManager.GetComponent<MetaDataComponent>(entityUid).EntityName
                };
                newMind.AddRole(new GhostRoleMarkerRole(newMind, entry.RoleName));

                newMind.ChangeOwningPlayer(session.UserId);
                newMind.TransferTo(entityUid);

                sessionIdx++;
                lotteryIdx++;

                successfulPlayers.Add(session);
                SendPlayerTakeoverCompleteEvent(session, entry.RoleName, entityUid);
            }

            if (lotteryIdx == lotteryCount)
            {
                // Role Group is fully used.
                _roleGroupEntries.Remove(entry.Identifier);
            }

            // Re-add remaining entities.
            while (lotteryIdx < lotteryCount)
            {
                var entityUid = lottery[lotteryIdx];
                entry.Entities.Add(entityUid);

                lotteryIdx++;
            }
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

    private void SendPlayerTakeoverCompleteEvent(IPlayerSession session, string roleName, EntityUid entity)
    {
        OnPlayerTakeoverComplete?.Invoke(new PlayerTakeoverCompleteEventArgs(session, roleName, entity));
    }
}
