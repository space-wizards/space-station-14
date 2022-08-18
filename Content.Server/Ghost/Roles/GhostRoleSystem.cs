using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Roles;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles;

[UsedImplicitly]
public sealed class GhostRoleSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;
    [Dependency] private readonly GhostRoleLotterySystem _ghostRoleLotterySystem = default!;

    private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

    /// <summary>
    /// Lookup ghost role components by the role name. This includes unavailable ghost roles.
    /// </summary>
    private readonly Dictionary<string, HashSet<GhostRoleComponent>> _ghostRoleLookup = new ();

    [ViewVariables]
    private IReadOnlyList<GhostRoleComponent> GhostRoleEntries => EntityQuery<GhostRoleComponent>().ToList();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GhostRoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<GhostRoleComponent, GhostRoleGroupEntityAttachedEvent>(OnGhostRoleGroupEntityAttached);
        SubscribeLocalEvent<GhostRoleComponent, GhostRoleGroupEntityDetachedEvent>(OnGhostRoleGroupEntityDetached);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _openMakeGhostRoleUis.Clear();
        _ghostRoleLookup.Clear();
    }

    private void OnMobStateChanged(EntityUid uid, GhostRoleComponent component, MobStateChangedEvent args)
    {
        switch (args.CurrentMobState)
        {
            case DamageState.Alive:
            {
                component.Damaged = false;
                break;
            }
            case DamageState.Critical:
            case DamageState.Dead:
                component.Damaged = true;
                _ghostRoleLotterySystem.UpdateAllEui();
                break;
            case DamageState.Invalid:
            default:
                break;
        }
    }

    public void OpenMakeGhostRoleEui(IPlayerSession session, EntityUid uid)
    {
        if (session.AttachedEntity == null)
            return;

        if (_openMakeGhostRoleUis.ContainsKey(session))
            CloseMakeGhostRoleEui(session);

        var eui = _openMakeGhostRoleUis[session] = new MakeGhostRoleEui(uid);
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void CloseMakeGhostRoleEui(IPlayerSession session)
    {
        if (_openMakeGhostRoleUis.Remove(session, out var eui))
        {
            eui.Close();
        }
    }

    public GhostRoleInfo[] GetGhostRolesInfo()
    {
        var ghostRoleInfo = new List<GhostRoleInfo>(_ghostRoleLookup.Count);
        var countData = new Dictionary<string, (int Lottery, int Takeover)>(_ghostRoleLookup.Count);

        foreach (var (roleName, components) in _ghostRoleLookup)
        {

            var (lotteryCount, takeoverCount) = (0, 0);
            foreach (var comp in components)
            {
                if (!comp.Available)
                    continue;

                if (comp.RoleLotteryEnabled)
                    lotteryCount++;
                else
                    takeoverCount++;
            }

            if(lotteryCount == 0 && takeoverCount == 0)
                continue;

            var firstComp = components.First();
            var role = new GhostRoleInfo()
            {
                Identifier = roleName,
                Name = roleName,
                Description = firstComp.RoleDescription,
                Rules = firstComp.RoleRules,
                AvailableLotteryRoleCount = lotteryCount,
                AvailableImmediateRoleCount = takeoverCount,
            };

            ghostRoleInfo.Add(role);
        }

        return ghostRoleInfo.ToArray();
    }

    public void GhostRoleInternalCreateMindAndTransfer(IPlayerSession player, EntityUid roleUid, EntityUid mob,
        GhostRoleComponent? role = null)
    {
        if (!Resolve(roleUid, ref role))
            return;

        var contentData = player.ContentData();

        DebugTools.AssertNotNull(contentData);

        var newMind = new Mind.Mind(player.UserId)
        {
            CharacterName = EntityManager.GetComponent<MetaDataComponent>(mob).EntityName
        };
        newMind.AddRole(new GhostRoleMarkerRole(newMind, role.RoleName));

        newMind.ChangeOwningPlayer(player.UserId);
        newMind.TransferTo(mob);
    }

    private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
    {
        component.Taken = true; // Handle take-overs outside of this system (e.g. Admin take-over).
        _ghostRoleLotterySystem.UpdateAllEui();
    }

    private void OnMindRemoved(EntityUid uid, GhostRoleComponent component, MindRemovedMessage args)
    {
        // Avoid re-registering it for duplicate entries and potential exceptions.
        if (!component.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
            return;

        component.Taken = false;
    }

    private void OnInit(EntityUid uid, GhostRoleComponent component, ComponentInit args)
    {
        if (component.Probability < 1f && !_random.Prob(component.Probability))
        {
            RemComp<GhostRoleComponent>(uid);
            return;
        }

        // Add the ghost role to the lookup dictionary.
        if (!_ghostRoleLookup.TryGetValue(component.RoleName, out var components))
            _ghostRoleLookup[component.RoleName] = components = new HashSet<GhostRoleComponent>();

        components.Add(component);

        if (component.RoleRules == "")
            component.RoleRules = Loc.GetString("ghost-role-component-default-rules");

        component.PendingLottery = true;
    }

    private void OnShutdown(EntityUid uid, GhostRoleComponent component, ComponentShutdown args)
    {
        _ghostRoleLookup.GetValueOrDefault(component.RoleName)?.Remove(component);
        _ghostRoleLotterySystem.UpdateAllEui();
    }

    private void OnGhostRoleGroupEntityAttached(EntityUid uid, GhostRoleComponent component, GhostRoleGroupEntityAttachedEvent args)
    {
        // Taken by a ghost role group. Mark as unavailable.
        component.GhostRoleGroupTaken = true;
        _ghostRoleLotterySystem.UpdateAllEui();
    }

    private void OnGhostRoleGroupEntityDetached(EntityUid uid, GhostRoleComponent component, GhostRoleGroupEntityDetachedEvent args)
    {
        component.GhostRoleGroupTaken = false;
        _ghostRoleLotterySystem.UpdateAllEui();
    }

    /// <summary>
    ///     Called by <see cref="GhostRoleLotterySystem"/> to have the lottery winners takeover the ghost roles.
    /// </summary>
    /// <param name="roleName">The ghost role to process requests for.</param>
    /// <param name="sessions">The players requesting roles</param>
    /// <param name="successTakeovers">Players that have successfully taken over a role.</param>
    /// <param name="rolesTaken">True if all roles are taken; Otherwise false.</param>
    public void OnLotteryResults(string roleName, IReadOnlyList<IPlayerSession> sessions,
        out List<IPlayerSession> successTakeovers, out bool rolesTaken)
    {
        successTakeovers = new List<IPlayerSession>();
        rolesTaken = true;

        var sessionIdx = 0;
        var sessionCount = sessions.Count;

        if (!_ghostRoleLookup.TryGetValue(roleName, out var components))
            return;

        foreach(var component in components)
        {
            if(!component.Available)
                continue;

            if (sessionIdx >= sessionCount)
                break; // All sessions have been used.

            if (!component.RoleLotteryEnabled)
                continue;

            var session = sessions[sessionIdx];
            if (!PerformTakeover(session, component))
                continue;

            OnPlayerTakeoverComplete(session, component);
            sessionIdx++;
        }
    }

    public void OnNextLotteryStarting()
    {
        foreach (var component in EntityQuery<GhostRoleComponent>())
        {
            if(component.Damaged || component.Taken || component.GhostRoleGroupTaken)
                continue;

            component.PendingLottery = false;
        }
    }

    public HashSet<string> GetAllAvailableLotteryItems()
    {
        var lotteryItems = new HashSet<string>();

        foreach (var (roleName, components) in _ghostRoleLookup)
        {
            var lotteryCount = 0;
            foreach (var comp in components)
            {
                if (comp.Available && comp.RoleLotteryEnabled)
                    lotteryCount++;
            }

            if (lotteryCount > 0)
                lotteryItems.Add(roleName);
        }

        return lotteryItems;
    }

    public void OnPlayerTakeoverComplete(IPlayerSession player, GhostRoleComponent comp)
    {
        if (player.AttachedEntity == null)
            return;

        _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {comp.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");
    }

    /// <summary>
    ///     Makes the player follow one of the entities in a group of ghost roles. The first request will
    ///     follow the first entity retrieved for the group. Subsequent requests will cycle through the entities as long
    ///     as the player does not stop following.
    /// </summary>
    /// <param name="player">The player to make follow.</param>
    /// <param name="roleName">The identifier for the group of ghost roles.</param>
    public void Follow(IPlayerSession player, string roleName)
    {
        if (player.AttachedEntity == null)
            return;

        if (!_ghostRoleLookup.TryGetValue(roleName, out var components))
            return;

        GhostRoleComponent? prev = null;
        var followComponent = CompOrNull<FollowerComponent>(player.AttachedEntity);
        var followedGhostRole = CompOrNull<GhostRoleComponent>(followComponent?.Following);

        // Search through the components. Find the first component that matches to start.
        using var componentEnumerator = components.GetEnumerator();
        while (componentEnumerator.MoveNext())
        {
            var component = componentEnumerator.Current;
            if (!component.Available)
                continue;

            prev = component;
            break;
        }

        if (prev == null)
            return; // No initial component found.

        if (followComponent == null || followedGhostRole?.RoleName != roleName)
        {
            _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, prev.Owner);
            return; // Wasn't currently following an entity that was part of the grouping.
        }

        // Player is currently following an entity in the same ghost role grouping. Find the next entity.
        var toFollow = prev;
        while (componentEnumerator.MoveNext())
        {
            var component = componentEnumerator.Current;
            if (!component.Available)
                continue;

            if (prev.Owner == followComponent.Following)
                toFollow = component;

            prev = component;
        }

        _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, toFollow.Owner);
    }

    /// <summary>
    ///     Attempts to perform a takeover for a player against the first entity found
    ///     in a group of ghost roles.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public bool RequestTakeover(IPlayerSession player, string roleName)
    {
        if (!_ghostRoleLookup.TryGetValue(roleName, out var components))
            return false;

        foreach(var component in components)
        {
            if(!component.Available)
                continue;

            if (!component.RoleLotteryEnabled)
                return PerformTakeover(player, component);
        }

        return false;
    }

    /// <summary>
    ///     Attempts to perform a takeover for a player against a specific ghost role.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    public bool PerformTakeover(IPlayerSession player, GhostRoleComponent role)
    {
        if (!role.Take(player))
            return false; // Currently only fails if the role is already taken.

        _ghostRoleLotterySystem.ClearPlayerLotteryRequests(player);
        _ghostRoleLotterySystem.UpdateAllEui();
        OnPlayerTakeoverComplete(player, role);
        return true;
    }

    /// <summary>
    ///     Returns the total number of available roles. If a ghost role has multiple uses, they will all be counted.
    /// </summary>
    public int GetAvailableCount()
    {
        var count = 0;

        foreach (var component in EntityQuery<GhostRoleComponent>())
        {
            if (!component.Available)
                continue;

            if (component is GhostRoleMobSpawnerComponent spawnerComponent)
                count += spawnerComponent.AvailableTakeovers;
            else
                count += 1;
        }

        return count;
    }

    public void OnGhostRoleUpdate(GhostRoleComponent component, string oldRoleName)
    {
        if (oldRoleName != component.RoleName)
        {
            if (_ghostRoleLookup.TryGetValue(oldRoleName, out var oldRoleNameLookup))
                oldRoleNameLookup.Remove(component);

            if (!_ghostRoleLookup.TryGetValue(component.RoleName, out var newRoleNameLookup))
                _ghostRoleLookup[component.RoleName] = newRoleNameLookup = new HashSet<GhostRoleComponent>();

            newRoleNameLookup.Add(component);
            component.PendingLottery = true; // Renaming a
        }

        _ghostRoleLotterySystem.UpdateAllEui();
    }
}



