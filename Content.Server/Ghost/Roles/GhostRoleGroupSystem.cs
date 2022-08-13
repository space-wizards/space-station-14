using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Roles;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// Handles ghost role groups. Entities assigned to a ghost role group are marked with a <see cref="GhostRoleGroupComponent"/>
/// </summary>
public sealed class GhostRoleGroupSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GhostRoleLotterySystem _ghostRoleLotterySystem = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    private readonly Dictionary<IPlayerSession, GhostRoleGroupsEui> _openUis = new();

    /// <summary>
    ///     Data for role group entries, indexed by the identifier acquired from <see cref="GhostRoleLotterySystem"/>
    /// </summary>
    private readonly Dictionary<uint, RoleGroupEntry> _roleGroupEntries = new();

    /// <summary>
    ///     Active role groups for each player. Entities placed down that fulfill certain criteria will
    ///     be automatically placed into the active role group.
    /// </summary>
    private readonly Dictionary<IPlayerSession, uint> _roleGroupActiveGroups = new();

    private bool _needsUpdateRoleGroups = true;

    private const string GhostRoleGroupPrefix = "GhostRoleGroupPrefix:";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, EntityPlacedEvent>(OnEntityPlaced);
        SubscribeLocalEvent<GhostRoleGroupComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GhostRoleGroupComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RequestAvailableLotteryItemsMessage>(OnLotteryRequest);
        SubscribeLocalEvent<GhostRoleGroupRequestTakeoverMessage>(OnTakeoverRequest);
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

    private void UpdatePlayerEui(IPlayerSession session)
    {
        if(!_openUis.TryGetValue(session, out var ui))
            return;

        ui.StateDirty();
    }

    public void UpdateAllEui()
    {
        foreach (var eui in _openUis.Values)
        {
            eui.StateDirty();
        }
    }

    public override void Update(float frameTime)
    {
        if (!_needsUpdateRoleGroups)
            return;

        _needsUpdateRoleGroups = false;
        UpdateAllEui();
    }

    /// <summary>
    ///     Retrieves the ghost role group data used for ghost roles UI.
    /// </summary>
    /// <returns></returns>
    public GhostRoleGroupInfo[] GetGhostRoleGroupsInfo()
    {
        var groups = new List<GhostRoleGroupInfo>(_roleGroupEntries.Count);
        foreach (var (_, group) in _roleGroupEntries)
        {
            if (group.Status != GhostRoleGroupStatus.Released)
                continue;

            groups.Add(new GhostRoleGroupInfo()
            {
                Identifier = group.Identifier,
                AvailableCount = group.Entities.Count,
                Name = group.RoleName,
                Description = group.RoleDescription,
                Status = group.Status.ToString(),
            });
        }

        return groups.ToArray();
    }

    /// <summary>
    ///     Retrieves the ghost role group data used for the role group management UI.
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
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

    private void OnEntityPlaced(EntityUid uid, GhostTakeoverAvailableComponent component, EntityPlacedEvent args)
    {
        var identifier = GetActiveGhostRoleGroupOrNull(args.PlacedBy);
        if (identifier == null)
            return;

        Logger.Debug($"Added {ToPrettyString(args.Placed)} to role group.");
        MakeSentientCommand.MakeSentient(uid, EntityManager);

        RemComp<GhostTakeoverAvailableComponent>(uid); // Remove this from ghost role system.
        var comp = AddComp<GhostRoleGroupComponent>(uid);
        comp.Identifier = identifier.Value;

        AttachToGhostRoleGroup(args.PlacedBy, identifier.Value, uid, component);
    }

    private void OnShutdown(EntityUid uid, GhostRoleGroupComponent role, ComponentShutdown args)
    {
        DetachFromGhostRoleGroup(role.Identifier, uid);
    }

    /// <summary>
    ///     Creates the role group and activate it for the user if they do not currently have
    ///     an active role group.
    /// </summary>
    /// <param name="session">The session of the creator.</param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <returns></returns>
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

        _needsUpdateRoleGroups = true;
        return identifier;
    }

    /// <summary>
    ///     Places the role group into a Releasing state.
    /// </summary>
    /// <param name="session">The session requesting the release.</param>
    /// <param name="identifier">The role group to release.</param>
    public void ReleaseGhostRoleGroup(IPlayerSession session, uint identifier)
    {
        if (!_adminManager.IsAdmin(session))
            return;

        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return;

        if (entry.Owner != session)
            return;

        entry.Status = GhostRoleGroupStatus.Releasing;
        _needsUpdateRoleGroups = true;
    }

    /// <summary>
    ///     Deletes the role group, either dissociating the entities or deleting them based on <paramref name="deleteEntities"/>
    /// </summary>
    /// <param name="session">The session requesting the deletion.</param>
    /// <param name="identifier">The role group to delete.</param>
    /// <param name="deleteEntities">If entities belonging to the role group should be deleted.</param>
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
        _needsUpdateRoleGroups = true;

        if (!deleteEntities)
            return;

        foreach (var entity in entry.Entities)
        {
            if (TryComp<MindComponent>(entity, out var mindComp) && mindComp.Mind?.UserId != null)
                continue; // Don't delete player-controlled entities.

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

            return;
        }

        _roleGroupActiveGroups[player] = identifier;
        UpdatePlayerEui(player);
    }

    /// <summary>
    ///     Get the session's active role group.
    /// </summary>
    /// <param name="session">The session to get the active role group for.</param>
    /// <returns>The active role group identifier if it exists and is active for the session. Otherwise null.</returns>
    public uint? GetActiveGhostRoleGroupOrNull(IPlayerSession session)
    {
        if (!_adminManager.IsAdmin(session))
            return null;

        return _roleGroupActiveGroups.GetValueOrDefault(session);
    }

    /// <summary>
    ///     Adds the entity to the role group. If the entity has a <see cref="GhostRoleComponent"/> then
    ///     it will be removed from the ghost role lottery.
    /// </summary>
    /// <param name="session">The session requesting the addition of the entity to the role group.</param>
    /// <param name="identifier">The identifier of the role group.</param>
    /// <param name="entity">The entity being added.</param>
    /// <param name="component">Ghost role component for bypassing component lookup.</param>
    /// <returns>true if the entity was successfully added; otherwise false.</returns>
    public bool AttachToGhostRoleGroup(IPlayerSession session, uint identifier, EntityUid entity, GhostRoleComponent? component = null)
    {
        if (!_adminManager.IsAdmin(session))
            return false;

        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return false;

        if (entry.Owner != session || entry.Status != GhostRoleGroupStatus.Editing)
            return false;

        if (entry.Entities.Contains(entity))
            return true;

        if (component != null || EntityManager.TryGetComponent(entity, out component))
            _ghostRoleLotterySystem.GhostRoleRemoveComponent(component); // Ghost role might already

        entry.Entities.Add(entity);
        _needsUpdateRoleGroups = true;
        return true;
    }

    /// <summary>
    ///     Removes the entity from the role group. If the role group becomes empty and isn't
    ///     being edited, it will be removed.
    /// </summary>
    /// <param name="identifier">The identifier of the role group.</param>
    /// <param name="entity">The entity being removed.</param>
    /// <returns>true if the entity was successfully removed; otherwise false</returns>
    public bool DetachFromGhostRoleGroup(uint identifier, EntityUid entity)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return false;

        var removed = entry.Entities.Remove(entity);
        if (!removed)
            return removed;

        _needsUpdateRoleGroups = true;

        if(entry.Entities.Count == 0 && entry.Status != GhostRoleGroupStatus.Editing)
            InternalDeleteGhostRoleGroup(entry, false);

        return removed;
    }

    /// <summary>
    ///     Event handler to supply <see cref="GhostRoleLotterySystem"/> with new lottery items. Any Releasing
    ///     role groups are put into the Released state and entered into the lottery.
    /// </summary>
    /// <param name="ev">Event to be supplied with new lottery items.</param>
    private void OnLotteryRequest(RequestAvailableLotteryItemsMessage ev)
    {
        var updated = false;

        foreach (var (identifier, group) in _roleGroupEntries)
        {
            if (group.Status != GhostRoleGroupStatus.Releasing)
                continue;

            group.Status = GhostRoleGroupStatus.Released;
            ev.GhostRoleGroups.Add(identifier);
            updated = true;
        }

        if(updated)
            UpdateAllEui();
    }

    /// <summary>
    ///     Event handler to perform the take-over of the role group entity.
    /// </summary>
    /// <param name="ev"></param>
    private void OnTakeoverRequest(GhostRoleGroupRequestTakeoverMessage ev)
    {
        if (!_roleGroupEntries.TryGetValue(ev.RoleGroupIdentifier, out var entry))
            return;

        if (entry.Entities.Count == 0)
        {
            ev.RoleGroupTaken = true;
            return;
        }

        var entityUid = entry.Entities.First();
        var contentData = ev.Player.ContentData();

        DebugTools.AssertNotNull(contentData);

        var newMind = new Mind.Mind(ev.Player.UserId)
        {
            CharacterName = EntityManager.GetComponent<MetaDataComponent>(entityUid).EntityName
        };
        newMind.AddRole(new GhostRoleMarkerRole(newMind, entry.RoleName));

        newMind.ChangeOwningPlayer(ev.Player.UserId);
        newMind.TransferTo(entityUid);

        ev.Result = true;
    }
}

/// <summary>
///     The state for a single role group. Role groups are owned by a player and can be in three states:
///     <list type="bullet">
///         <item><see cref="GhostRoleGroupStatus.Editing"/> - The role group can have entities added to it and is not available for lottery.</item>
///         <item><see cref="GhostRoleGroupStatus.Releasing"/> - The role group is waiting to be added into a lottery. Entities can no longer be added.</item>
///         <item><see cref="GhostRoleGroupStatus.Released"/> - The role group is now in a lottery. Player's can request to enter the lottery.</item>
///     </list>
/// </summary>
internal sealed class RoleGroupEntry
{
    public uint Identifier { get; init; }
    public IPlayerSession Owner { get; init; } = default!;
    public string RoleName { get; init; } = default!;
    public string RoleDescription { get; init; } = default!;

    public GhostRoleGroupStatus Status = GhostRoleGroupStatus.Editing;

    public readonly List<EntityUid> Entities = new();
}

/// <summary>
///     Raised when a system needs a role group to perform a take over on one of it's owned entities.
///     TODO: Method events bad. ES methods good.
/// </summary>
public sealed class GhostRoleGroupRequestTakeoverMessage : EntityEventArgs
{
    /// <summary>
    ///     Player to takeover the entity.
    ///     Input parameter.
    /// </summary>
    public IPlayerSession Player { get; }

    /// <summary>
    ///     Identifier for the role group.
    ///     Input paramter.
    /// </summary>
    public uint RoleGroupIdentifier { get; }

    /// <summary>
    ///     If the player successfully took over the entity.
    ///     Output parameter.
    /// </summary>
    public bool Result { get; set; }

    /// <summary>
    ///     If the role group was consumed when being taken over.
    ///     Output parameter.
    /// </summary>
    public bool RoleGroupTaken { get; set; }

    public GhostRoleGroupRequestTakeoverMessage(IPlayerSession player, uint identifier)
    {
        Player = player;
        RoleGroupIdentifier = identifier;
    }
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
