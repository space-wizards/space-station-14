using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
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

    /// <summary>
    ///     Cache of <see cref="GhostRoleGroupComponent"/> indexed by the role group identifier.
    /// </summary>
    private readonly Dictionary<uint, HashSet<GhostRoleGroupComponent>> _lookupComponentsByIdentifier = new();

    private bool _needsUpdateRoleGroups = true;

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
        SubscribeLocalEvent<GhostRoleCountRequestedEvent>(OnGhostRoleCountRequest);
    }

    private void OnMindAdded(EntityUid uid, GhostRoleGroupComponent component, MindAddedMessage args)
    {
        RemComp<GhostRoleGroupComponent>(uid); // This will trigger the removal of the entity from the role group.
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        foreach (var session in _openUis.Keys)
        {
            CloseEui(session);
        }

        _openUis.Clear();
        _roleGroupActiveGroups.Clear();
        _roleGroupEntries.Clear();
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
            eui.Close();
    }

    private void UpdatePlayerEui(IPlayerSession session)
    {
        if(!_openUis.TryGetValue(session, out var ui))
            return;

        ui.StateDirty();
    }

    private void UpdateAllEui()
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
                AvailableCount = EntityQuery<GhostRoleGroupComponent>().Count(c => c.Identifier == group.Identifier),
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
            var groupEntities = EntityQuery<GhostRoleGroupComponent>()
                .Where(c => c.Identifier == entry.Identifier)
                .Select(c => c.Owner).ToArray();

            var group = new AdminGhostRoleGroupInfo()
            {
                GroupIdentifier = entry.Identifier,
                Name = entry.RoleName,
                Description = entry.RoleDescription,
                Status = entry.Status,
                Entities = groupEntities,
                IsActive = _roleGroupActiveGroups.GetValueOrDefault(session) == entry.Identifier,
                CanModify = CanModify(session, entry)
            };

            groups.Add(group);
        }

        return groups.ToArray();
    }

    private void OnEntityPlaced(EntityUid uid, GhostTakeoverAvailableComponent component, EntityPlacedEvent args)
    {
        if (!TryGetActiveRoleGroup(args.PlacedBy, out var identifier))
            return;

        RemComp<GhostTakeoverAvailableComponent>(uid); // Remove so that GhostRoleSystem doesn't manage it simultaneously.
        AttachToGhostRoleGroup(args.PlacedBy, identifier, uid, component);
    }

    private void OnShutdown(EntityUid uid, GhostRoleGroupComponent role, ComponentShutdown args)
    {
        if (_lookupComponentsByIdentifier.TryGetValue(role.Identifier, out var components))
            components.Remove(role);

        if (!_roleGroupEntries.TryGetValue(role.Identifier, out var entry))
            return;

        _needsUpdateRoleGroups = true;

        if (entry.Status == GhostRoleGroupStatus.Editing)
            return;

        if(EntityQuery<GhostRoleGroupComponent>().All(c => c.Identifier != entry.Identifier))
            InternalDeleteGhostRoleGroup(entry, false);
        else
            _ghostRoleLotterySystem.UpdateAllEui();
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
        _lookupComponentsByIdentifier.Add(identifier, new HashSet<GhostRoleGroupComponent>());
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

        if (!_roleGroupEntries.TryGetValue(identifier, out var entry) || !CanModify(session, entry))
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

        if (!_roleGroupEntries.TryGetValue(identifier, out var entry) || !CanModify(session, entry))
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
        _ghostRoleLotterySystem.UpdateAllEui();

        _lookupComponentsByIdentifier.Remove(entry.Identifier, out var deleteComponents);

        if (!deleteEntities || deleteComponents == null)
            return;

        foreach (var groupComp in deleteComponents)
        {
            if (TryComp<MindComponent>(groupComp.Owner, out var mindComp) && mindComp.Mind?.UserId != null)
                continue; // Don't delete player-controlled entities.

            // TODO: Exempt certain entities from this.
            EntityManager.QueueDeleteEntity(groupComp.Owner);
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
        if (!_roleGroupEntries.TryGetValue(identifier, out var nextRoleGroup) || !CanModify(player, nextRoleGroup))
            return;

        // Deactivate the current role group first.
        if (_roleGroupActiveGroups.Remove(player, out var current) && current == identifier)
        {
            UpdatePlayerEui(player); // Player toggled the current active role group.
            return;
        }

        _roleGroupActiveGroups[player] = identifier;
        UpdatePlayerEui(player);
    }

    /// <summary>
    ///     Get the session's active role group.
    /// </summary>
    /// <param name="player">The session to get the active role group for.</param>
    /// <param name="groupIdentifier">The identifier of the active role group.</param>
    /// <returns>True if the session has an active role group; Otherwise false.</returns>
    public bool TryGetActiveRoleGroup(IPlayerSession player, out uint groupIdentifier)
    {
        return _roleGroupActiveGroups.TryGetValue(player, out groupIdentifier);
    }

    /// <summary>
    ///     Checks if the player can modify the role group.
    /// </summary>
    /// <param name="player">The session to check access for.</param>
    /// <param name="groupIdentifier">The identifier of the role group to check.</param>
    /// <returns>true if the player can modify the role group; Otherwise false.</returns>
    public bool CanModify(IPlayerSession player, uint groupIdentifier)
    {
        return _roleGroupEntries.TryGetValue(groupIdentifier, out var roleGroupEntry)
               && CanModify(player, roleGroupEntry);
    }

    /// <summary>
    ///     Checks if the player can modify the role group.
    /// </summary>
    /// <param name="player">The session to check access for.</param>
    /// <param name="entry">The role group to check.</param>
    /// <returns>true if the player can modify the role group; Otherwise false.</returns>
    private bool CanModify(IPlayerSession player, RoleGroupEntry entry)
    {
        return _adminManager.IsAdmin(player) && entry.Owner == player;
    }

    /// <summary>
    ///     Adds the entity to the role group. If the entity has a <see cref="GhostRoleComponent"/> then
    ///     it will be removed from the ghost role lottery.
    /// </summary>
    /// <param name="session">The session requesting the addition of the entity to the role group.</param>
    /// <param name="identifier">The identifier of the role group.</param>
    /// <param name="entity">The entity being added.</param>
    /// <param name="ghostRoleComponent">Ghost role component for bypassing component lookup.</param>
    /// <returns>true if the entity was successfully added; otherwise false.</returns>
    public bool AttachToGhostRoleGroup(IPlayerSession session, uint identifier, EntityUid entity, GhostRoleComponent? ghostRoleComponent = null)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry) || !CanModify(session, entry))
            return false;

        if (entry.Status != GhostRoleGroupStatus.Editing)
            return false;

        uint? prevRoleGroupIdentifier = null; // Get the previous role group (if it exists).
        if (TryComp<GhostRoleGroupComponent>(entity, out var ghostRoleGroupComp))
        {
            if (ghostRoleGroupComp.Identifier == identifier)
                return true; // Already is part of this role group.

            prevRoleGroupIdentifier = ghostRoleGroupComp.Identifier;
            if (!CanModify(session, ghostRoleGroupComp.Identifier))
                return false; // Can't transfer from a role group we don't have the ability to modify.
        }

        // If it has ghost role, it may already be part of a lottery. Remove it.
        if (ghostRoleComponent != null || EntityManager.TryGetComponent(entity, out ghostRoleComponent))
            _ghostRoleLotterySystem.GhostRoleRemoveComponent(ghostRoleComponent);

        MakeSentientCommand.MakeSentient(entity, EntityManager);
        var groupComponent = EnsureComp<GhostRoleGroupComponent>(entity);
        groupComponent.Identifier = identifier;
        _needsUpdateRoleGroups = true;

        // Remove from previous role group lookup.
        if (prevRoleGroupIdentifier != null &&
            _lookupComponentsByIdentifier.TryGetValue(prevRoleGroupIdentifier.Value, out var components))
        {
            components.Remove(groupComponent);
        }

        // Add the component to lookup tables.
        if(!_lookupComponentsByIdentifier.TryGetValue(groupComponent.Identifier, out components))
            _lookupComponentsByIdentifier[groupComponent.Identifier] = components = new HashSet<GhostRoleGroupComponent>();

        components.Add(groupComponent);
        return true;
    }

    /// <summary>
    ///     Removes the entity from the role group. If the entity has attached to a different role group, it will remain
    ///     attached to that role group.
    ///     <para/>
    ///
    ///     This will fail if the role group has been released or is not owned by the session. If you need to
    ///     force removal, remove the <see cref="GhostRoleGroupComponent"/> instead.
    /// </summary>
    /// <param name="session">The player session requesting the detach.</param>
    /// <param name="identifier">The identifier of the role group.</param>
    /// <param name="entity">The entity being removed.</param>
    /// <returns>true if the entity was successfully removed or not a part of the role group; otherwise false</returns>
    public bool DetachFromGhostRoleGroup(IPlayerSession session, uint identifier, EntityUid entity)
    {
        if (!_roleGroupEntries.TryGetValue(identifier, out var entry))
            return true;

        if (TryComp<GhostRoleGroupComponent>(entity, out var currentGhostRoleGroup) && currentGhostRoleGroup.Identifier != identifier)
            return true; // Entity has already been moved to a different role group.

        if (entry.Status != GhostRoleGroupStatus.Editing || !CanModify(session, entry))
            return false;

        RemComp<GhostRoleGroupComponent>(entity); // Triggers the removal of the entity from the role group.
        return true;
    }

    private void OnGhostRoleCountRequest(GhostRoleCountRequestedEvent ev)
    {
        foreach (var (_, group) in _roleGroupEntries)
        {
            if (group.Status != GhostRoleGroupStatus.Released)
                continue;

            ev.Count += _lookupComponentsByIdentifier[group.Identifier].Count;
        }
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

        if (!_lookupComponentsByIdentifier.TryGetValue(ev.RoleGroupIdentifier, out var components)
            || components.Count == 0)
        {
            ev.RoleGroupTaken = true; // Role group is already completely used up.
            return;
        }

        var ghostRoleGroupComponent = components.First();
        var entityUid = ghostRoleGroupComponent.Owner;
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
    ///     Input parameter.
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
start <name> <description>
attach <entity> <groupIdentifier>
detach <entity> <groupIdentifier>
delete <deleteEntities> <groupIdentifier>
release <groupIdentifier>
open";

    private void ExecuteStart(IConsoleShell shell, IPlayerSession player, IReadOnlyList<string> args)
    {
        if (args.Count != 3)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        var name = args[1];
        var description = args[2];

        var manager = EntitySystem.Get<GhostRoleGroupSystem>();
        var id = manager.StartGhostRoleGroup(player, name, description);
        shell.WriteLine(id != null ? $"Role group started. Id is: {id}" : "Failed to start role group");
    }

    private void ExecuteAttach(IConsoleShell shell, IPlayerSession player, IReadOnlyList<string> args)
    {
        if (args.Count != 3)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        var manager = EntitySystem.Get<GhostRoleGroupSystem>();

        if (!EntityUid.TryParse(args[1], out var entityUid) || !entityUid.Valid)
        {
            shell.WriteLine("Invalid argument for <entity>. Expected EntityUid");
            return;
        }

        if (!uint.TryParse(args[2], out var groupIdentifier))
        {
            shell.WriteLine("Invalid argument for <groupIdentifier>. Expected unsigned integer");
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();
        if (!entityManager.EntityExists(entityUid))
        {
            shell.WriteLine("Invalid entity specified!");
            return;
        }


        shell.WriteLine(manager.AttachToGhostRoleGroup(player, groupIdentifier, entityUid)
            ? "Successfully attached entity to role group"
            : "Failed to attach entity to role group.");
    }

    private void ExecuteDetach(IConsoleShell shell, IPlayerSession player, IReadOnlyList<string> args)
    {
        if (args.Count != 3)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        var manager = EntitySystem.Get<GhostRoleGroupSystem>();

        if (!EntityUid.TryParse(args[1], out var entityUid) || !entityUid.Valid)
        {
            shell.WriteLine("Invalid argument for <entity>. Expected EntityUid");
            return;
        }

        if (!uint.TryParse(args[2], out var groupIdentifier))
        {
            shell.WriteLine("Invalid argument for <groupIdentifier>. Expected unsigned integer.");
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();
        if (!entityManager.EntityExists(entityUid))
        {
            shell.WriteLine("Invalid entity specified!");
            return;
        }

        shell.WriteLine(manager.DetachFromGhostRoleGroup(player, groupIdentifier, entityUid)
            ? "Successfully detached entity from role group."
            : "Failed to detach entity from role group");
    }

    private void ExecuteDelete(IConsoleShell shell, IPlayerSession player, IReadOnlyList<string> args)
    {
        var manager = EntitySystem.Get<GhostRoleGroupSystem>();
        if (args.Count != 3)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        if (!bool.TryParse(args[1], out var deleteEntities))
        {
            shell.WriteLine("Invalid argument for <deleteEntities>. Expected true|false");
        }

        if (!uint.TryParse(args[2], out var identifier))
        {
            shell.WriteLine("Invalid argument for <groupIdentifier>. Expected unsigned integer.");
            return;
        }

        manager.DeleteGhostRoleGroup(player, identifier, deleteEntities);
    }

    private void ExecuteRelease(IConsoleShell shell,  IPlayerSession player, IReadOnlyList<string> args)
    {
        var manager = EntitySystem.Get<GhostRoleGroupSystem>();
        if (args.Count != 2)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        if (!uint.TryParse(args[1], out var identifier))
        {
            shell.WriteLine("Invalid argument for <groupIdentifier>. Expected unsigned integer.");
            return;
        }

        manager.ReleaseGhostRoleGroup(player, identifier);
    }

    private void ExecuteOpen(IPlayerSession player)
    {
        EntitySystem.Get<GhostRoleGroupSystem>().OpenEui(player);
    }

    private void ExecuteActivate(IConsoleShell shell, IPlayerSession player, IReadOnlyList<string> args)
    {
        var manager = EntitySystem.Get<GhostRoleGroupSystem>();
        if (args.Count != 2)
            return;

        if (!uint.TryParse(args[1], out var identifier))
        {
            shell.WriteLine("Invalid argument for <groupIdentifier>. Expected unsigned integer.");
            return;
        }

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
                ExecuteStart(shell, player, args);
                break;
            case "attach":
                ExecuteAttach(shell, player, args);
                break;
            case "detach":
                ExecuteDetach(shell, player, args);
                break;
            case "activate":
                ExecuteActivate(shell, player, args);
                break;
            case "release":
                ExecuteRelease(shell, player, args);
                break;
            case "delete":
                ExecuteDelete(shell, player, args);
                break;
            case "open":
                ExecuteOpen(player);
                break;
            default:
                shell.WriteLine($"Usage: {Help}");
                break;
        }
    }
}
