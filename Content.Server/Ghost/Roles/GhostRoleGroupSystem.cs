using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// This handles...
/// </summary>
public sealed class GhostRoleGroupSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GhostRoleLotterySystem _ghostRoleLotterySystem = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    private readonly Dictionary<IPlayerSession, GhostRoleGroupsEui> _openUis = new();

    private readonly Dictionary<uint, RoleGroupEntry> _roleGroupEntries = new();
    private readonly Dictionary<IPlayerSession, uint> _roleGroupActiveGroups = new();

    private const string GhostRoleGroupPrefix = "GhostRoleGroupPrefix:";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<GhostRoleComponent, EntityPlacedEvent>(OnEntityPlaced);
        SubscribeLocalEvent<GhostRoleGroupComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GhostRoleGroupComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnGhostRoleGroupChanged(GhostRoleGroupChangedEventArgs ev)
    {
        UpdateAllEui();
    }

    private void OnMindAdded(EntityUid uid, GhostRoleGroupComponent component, MindAddedMessage args)
    {
        DetachFromGhostRoleGroup(component.Identifier, uid);
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        foreach (var session in _openUis.Keys)
        {
            CloseEui(session);
        }

        _openUis.Clear();
    }

    public void OpenEui(IPlayerSession session)
    {
        if(_openUis.ContainsKey(session))
            CloseEui(session);

        var eui = _openUis[session] = new GhostRoleGroupsEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void CloseEui(IPlayerSession session)
    {
        if(_openUis.Remove(session, out var eui))
            eui?.Close();
    }

    public void UpdateAllEui()
    {
        foreach (var eui in _openUis.Values)
        {
            eui.StateDirty();
        }
    }

    public GhostRoleGroupInfo[] GetGhostRoleGroupsInfo(IPlayerSession session)
    {
        var groups = new List<GhostRoleGroupInfo>(_roleGroupEntries.Count);
        foreach (var (_, group) in _roleGroupEntries)
        {
            if (group.Status != GhostRoleGroupStatus.Released)
                continue;

            groups.Add(new GhostRoleGroupInfo()
            {
                GroupIdentifier = group.Identifier,
                Identifier = $"{GhostRoleGroupPrefix}{group.Identifier}",
                AvailableCount = group.Entities.Count,
                Name = group.RoleName,
                Description = group.RoleDescription,
                Status = group.Status.ToString(),
                IsRequested = group.Requests.Contains(session),
            });
        }

        return groups.ToArray();
    }

    public AdminGhostRoleGroupInfo[] GetAdminGhostRoleGroupInfo(IPlayerSession session)
    {
        var groups = new List<AdminGhostRoleGroupInfo>(_roleGroupEntries.Count);
        if (!_adminManager.IsAdmin(session))
            return groups.ToArray();

        foreach (var (_, entry) in _roleGroupEntries)
        {
            var group = new AdminGhostRoleGroupInfo()
            {
                GroupIdentifier = entry.Identifier,
                Name = entry.RoleName,
                Description = entry.RoleDescription,
                Status = entry.Status,
                Entities = entry.Entities.ToArray(),
                IsActive = _roleGroupActiveGroups.GetValueOrDefault(session) == entry.Identifier,
            };

            groups.Add(group);
        }

        return groups.ToArray();
    }

    private void OnEntityPlaced(EntityUid uid, GhostRoleComponent component, EntityPlacedEvent args)
    {
        var identifier = GetActiveGhostRoleGroupOrNull(args.PlacedBy);
        if (identifier == null)
            return;

        Logger.Debug($"Added {ToPrettyString(args.Placed)} to role group.");
        MakeSentientCommand.MakeSentient(uid, EntityManager);

        var comp = AddComp<GhostRoleGroupComponent>(uid);
        comp.Identifier = identifier.Value;

        AttachToGhostRoleGroup(args.PlacedBy, identifier.Value, uid, component);
    }

    private void OnShutdown(EntityUid uid, GhostRoleGroupComponent role, ComponentShutdown args)
    {
        DetachFromGhostRoleGroup(role.Identifier, uid);
    }

    public uint? StartGhostRoleGroup(IPlayerSession session, string name, string description)
    {
        if (!_adminManager.IsAdmin(session))
            return null;

        var identifier = _ghostRoleLotterySystem.NextIdentifier;
        var entry = new RoleGroupEntry()
        {
            Owner = session,
            Identifier = identifier,
            RoleName = name,
            RoleDescription = description,
        };

        _roleGroupEntries.Add(identifier, entry);
        if(!_roleGroupActiveGroups.ContainsKey(session))
            _roleGroupActiveGroups.Add(session, entry.Identifier);

        SendGhostRoleGroupChangedEvent(false);
        // OnGhostRoleGroupChanged();
        return identifier;
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
        SendGhostRoleGroupChangedEvent();
    }

    public void DeleteGhostRoleGroup(IPlayerSession session, uint identifier, bool deleteEntities)
    {
        if (!_adminManager.IsAdmin(session))
            return;

        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return;

        if (entry.Owner != session)
            return;

        InternalDeleteGhostRoleGroup(entry, deleteEntities);
    }

    private void InternalDeleteGhostRoleGroup(RoleGroupEntry entry, bool deleteEntities)
    {
        // Clear active group roles.
        foreach (var (k, v) in _roleGroupActiveGroups)
        {
            if (v == entry.Identifier)
                _roleGroupActiveGroups.Remove(k);
        }

        _roleGroupEntries.Remove(entry.Identifier);
        SendGhostRoleGroupChangedEvent(wasDeleted: true);

        if (!deleteEntities)
            return;

        foreach (var entity in entry.Entities)
        {
            // TODO: Exempt certain entities from this.
            EntityManager.QueueDeleteEntity(entity);
        }
    }

    /// <summary>
    ///     Set the players active role group. If the role group is already active, it is
    ///     deactivated instead.
    /// </summary>
    /// <param name="player">The session to activate the role group for.</param>
    /// <param name="identifier">The role group to activate/deactivate.</param>
    public void ToggleOrSetActivePlayerRoleGroup(IPlayerSession player, uint identifier)
    {
        if (!_roleGroupEntries.ContainsKey(identifier))
            return;

        if (_roleGroupActiveGroups.Remove(player, out var current) && current == identifier)
        {
            SendGhostRoleGroupChangedEvent();
            return;
        }

        _roleGroupActiveGroups[player] = identifier;
        SendGhostRoleGroupChangedEvent();
    }

    public uint? GetActiveGhostRoleGroupOrNull(IPlayerSession session)
    {
        if (!_adminManager.IsAdmin(session))
            return null;

        return _roleGroupActiveGroups.GetValueOrDefault(session);
    }

    public bool AttachToGhostRoleGroup(IPlayerSession session, uint identifier, EntityUid entity, GhostRoleComponent? component = null)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return false;

        if (entry.Owner != session || entry.Status != GhostRoleGroupStatus.Editing)
            return false;

        if (entry.Entities.Contains(entity))
            return true;

        // if (component != null || EntityManager.TryGetComponent(entity, out component))
        //     _ghostRoleLotterySystem.RemoveGhostRoleLotteryRequest(session, component);

        entry.Entities.Add(entity);
        SendGhostRoleGroupChangedEvent();
        return true;
    }

    public bool DetachFromGhostRoleGroup(uint identifier, EntityUid uid)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return false;

        var removed = entry.Entities.Remove(uid);
        if (!removed)
            return removed;

        SendGhostRoleGroupChangedEvent();

        if(entry.Entities.Count == 0 && entry.Status != GhostRoleGroupStatus.Editing)
            InternalDeleteGhostRoleGroup(entry, false);

        return removed;
    }

    private void SendGhostRoleGroupChangedEvent(bool wasReleased = false, bool wasDeleted = false)
    {
        // OnGhostRoleGroupChanged?.Invoke(new GhostRoleGroupChangedEventArgs() { WasReleased = wasReleased});
    }
}

internal sealed class RoleGroupEntry
{
    public uint Identifier { get; init; }
    public IPlayerSession Owner { get; init; } = default!;
    public string RoleName { get; init; } = default!;
    public string RoleDescription { get; init; } = default!;

    public GhostRoleGroupStatus Status = GhostRoleGroupStatus.Editing;

    public readonly List<EntityUid> Entities = new();

    public readonly HashSet<IPlayerSession> Requests = new();
}

public struct GhostRoleGroupChangedEventArgs
{
    /// <summary>
    ///     Has the role group been released, allowing players to enter its lottery?
    /// </summary>
    public bool WasReleased { get; init; }

    /// <summary>
    ///     Has the role group been deleted?
    /// </summary>
    public bool WasDeleted { get; init; }
}

[AdminCommand(AdminFlags.Spawn)]
public sealed class GhostRoleGroupsCommand : IConsoleCommand
{
    public string Command => "ghostrolegroups";
    public string Description => "Manage ghost role groups.";
    public string Help => @$"${Command}
start <name> <description> <rules>
delete <deleteEntities> <groupIdentifier>
release [groupIdentifier]
open";

    private void ExecuteStart(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
    {
        if (args.Length < 3)
            return;

        var manager = EntitySystem.Get<GhostRoleGroupSystem>();

        var name = args[1];
        var description = args[2];

        var id = manager.StartGhostRoleGroup(player, name, description);
        shell.WriteLine($"Role group start: {id}");
    }

    private void ExecuteDelete(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
    {
        var manager = EntitySystem.Get<GhostRoleGroupSystem>();
        if (args.Length != 3)
            return;

        var deleteEntities = bool.Parse(args[1]);
        var identifier = uint.Parse(args[2]);

        manager.DeleteGhostRoleGroup(player, identifier, deleteEntities);
    }

    private void ExecuteRelease(IConsoleShell shell,  IPlayerSession player, string argStr, string[] args)
    {
        var manager = EntitySystem.Get<GhostRoleGroupSystem>();

        switch (args.Length)
        {
            case > 2:
                shell.WriteLine(Help);
                break;
            case 2:
            {
                var identifier = uint.Parse(args[1]);
                manager.ReleaseGhostRoleGroup(player, identifier);
                break;
            }
            default:
            {
                var identifier = manager.GetActiveGhostRoleGroupOrNull(player);
                if(identifier != null)
                    manager.ReleaseGhostRoleGroup(player, identifier.Value);
                break;
            }
        }
    }

    private void ExecuteOpen(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
    {
        EntitySystem.Get<GhostRoleGroupSystem>().OpenEui(player);
    }

    private void ExecuteActivate(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
    {
        var manager = EntitySystem.Get<GhostRoleGroupSystem>();
        if (args.Length != 2)
            return;

        var identifier = uint.Parse(args[1]);

        manager.ToggleOrSetActivePlayerRoleGroup(player, identifier);
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteLine("You can only manage ghost role groups on a client.");
            return;
        }

        if (args.Length < 1)
        {
            shell.WriteLine($"Usage: {Help}");
            return;
        }

        var player = (IPlayerSession) shell.Player;

        switch (args[0])
        {
            case "start":
                ExecuteStart(shell, player, argStr, args);
                break;
            case "activate":
                ExecuteActivate(shell, player, argStr, args);
                break;
            case "release":
                ExecuteRelease(shell, player, argStr, args);
                break;
            case "delete":
                ExecuteDelete(shell, player, argStr, args);
                break;
            case "open":
                ExecuteOpen(shell, player, argStr, args);
                break;
            default:
                shell.WriteLine($"Usage: {Help}");
                break;
        }
    }
}
