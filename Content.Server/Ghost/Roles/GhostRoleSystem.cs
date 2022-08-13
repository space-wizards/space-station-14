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
using Content.Shared.Ghost.Roles;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles;

using GhostRoleCompId = UInt32;

internal record struct GhostRoleData
{
    public uint RoleIdentifier;
    public string RoleName;
    public string RoleDescription;
    public string RoleRules;
    public HashSet<GhostRoleComponent> Components;
}

[UsedImplicitly]
public sealed class GhostRoleSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;
    [Dependency] private readonly GhostRoleLotterySystem _ghostRoleLotterySystem = default!;

    private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

    /// <summary>
    ///     Ghost roles pending assignment to a group of roles. This is important as the component might be immediately
    ///     removed (<see cref="GhostRoleGroupSystem"/>) or modified (<see cref="MakeGhostRoleCommand"/>).
    /// </summary>
    private readonly HashSet<GhostRoleComponent> _queuedComponents = new();

    /// <summary>
    ///     Cache mapping the identifiers back to the components for quick lookup.
    /// </summary>
    private readonly Dictionary<string, GhostRoleData> _ghostRoleData = new ();

    [ViewVariables]
    private IReadOnlyDictionary<string, GhostRoleData> GhostRoleEntries => _ghostRoleData;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GhostRoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<GhostRoleComponent, GhostRoleRequestTakeoverMessage>(OnTakeoverRequest);
        SubscribeLocalEvent<RequestAvailableLotteryItemsMessage>(OnLotteryRequest);
        SubscribeLocalEvent<GhostRoleCountRequestedEvent>(OnRequestCount);
    }

    private void OnMobStateChanged(EntityUid uid, GhostRoleComponent component, MobStateChangedEvent args)
    {
        switch (args.CurrentMobState)
        {
            case DamageState.Alive:
            {
                if (!component.Taken)
                    _queuedComponents.Add(component);
                break;
            }
            case DamageState.Critical:
            case DamageState.Dead:
                _queuedComponents.Remove(component);
                _ghostRoleLotterySystem.GhostRoleRemoveComponent(component);
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
        var ghostRoleInfo = new List<GhostRoleInfo>(_ghostRoleData.Count);

        foreach (var (roleIdentifier, data) in _ghostRoleData)
        {
            var lotteryCount = 0;
            var takeoverCount = 0;

            foreach (var comp in data.Components)
            {
                if (comp.Queued)
                    continue;

                var count = comp is GhostRoleMobSpawnerComponent spawn ? spawn.AvailableTakeovers : 1;

                if (comp.RoleUseLottery)
                    lotteryCount += count;
                else
                    takeoverCount += count;
            }

            if(lotteryCount == 0 && takeoverCount == 0)
                continue;

            var role = new GhostRoleInfo()
            {
                Identifier = roleIdentifier,
                Name = data.RoleName,
                Description = data.RoleDescription,
                Rules = data.RoleRules,
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
        _queuedComponents.Remove(component);
        if (_ghostRoleData.TryGetValue(component.RoleName, out var data))
            data.Components.Remove(component);


        _ghostRoleLotterySystem.GhostRoleRemoveComponent(component);
    }

    private void OnMindRemoved(EntityUid uid, GhostRoleComponent component, MindRemovedMessage args)
    {
        // Avoid re-registering it for duplicate entries and potential exceptions.
        if (!component.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
            return;

        component.Taken = false;
        _queuedComponents.Add(component);
    }

    private void OnInit(EntityUid uid, GhostRoleComponent component, ComponentInit args)
    {
        if (component.Probability < 1f && !_random.Prob(component.Probability))
        {
            RemComp<GhostRoleComponent>(uid);
            return;
        }

        if (component.RoleRules == "")
            component.RoleRules = Loc.GetString("ghost-role-component-default-rules");

        component.Queued = true;

        _queuedComponents.Add(component);
    }

    private void OnShutdown(EntityUid uid, GhostRoleComponent component, ComponentShutdown args)
    {
        _queuedComponents.Remove(component);
        _ghostRoleLotterySystem.GhostRoleRemoveComponent(component);

        if (_ghostRoleData.TryGetValue(component.RoleName, out var data))
            data.Components.Remove(component);
    }

    private void OnTakeoverRequest(EntityUid uid, GhostRoleComponent component, GhostRoleRequestTakeoverMessage ev)
    {
        ev.Result = RequestTakeover(ev.Player, ev.GhostRole);
        if (!ev.Result)
            return;

        ev.GhostRoleTaken = ev.GhostRole.Taken;
        OnPlayerTakeoverComplete(ev.Player, ev.GhostRole.RoleName);
    }

    private void OnLotteryRequest(RequestAvailableLotteryItemsMessage ev)
    {
        foreach (var comp in _queuedComponents)
        {
            comp.Queued = false;
            ev.GhostRoles.Add(comp);

            if (!_ghostRoleData.TryGetValue(comp.RoleName, out var data))
            {
                _ghostRoleData[comp.RoleName] = data = new GhostRoleData()
                {
                    RoleIdentifier = _ghostRoleLotterySystem.NextIdentifier,
                    RoleName = comp.RoleName,
                    RoleDescription = comp.RoleDescription,
                    RoleRules = comp.RoleRules,
                    Components = new HashSet<GhostRoleComponent>(),
                };
            }

            comp.Identifier = data.RoleIdentifier;
            data.Components.Add(comp);
        }

        _queuedComponents.Clear();
    }

    private void OnRequestCount(GhostRoleCountRequestedEvent ev)
    {
        foreach (var (_, data) in _ghostRoleData)
        {
            if (data.Components.Count == 0)
                return;

            foreach (var comp in data.Components)
            {
                if (comp is GhostRoleMobSpawnerComponent spawnComp)
                    ev.Count += spawnComp.AvailableTakeovers;
                else
                    ev.Count += 1;
            }
        }
    }

    public void OnPlayerTakeoverComplete(IPlayerSession player, string roleIdentifier)
    {
        if (player.AttachedEntity == null || !_ghostRoleData.TryGetValue(roleIdentifier, out var data))
            return;

        _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {data.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");
    }

    /// <summary>
    ///     Makes the player follow one of the entities in a group of ghost roles. The first request will
    ///     follow the first entity retrieved for the group. Subsequent requests will cycle through the entities as long
    ///     as the player does not stop following.
    /// </summary>
    /// <param name="player">The player to make follow.</param>
    /// <param name="roleIdentifier">The identifier for the group of ghost roles.</param>
    public void Follow(IPlayerSession player, string roleIdentifier)
    {
        if (player.AttachedEntity == null)
            return;

        if (!_ghostRoleData.TryGetValue(roleIdentifier, out var data))
            return;

        if (data.Components.Count == 0)
            return;

        var roleToFollow = data.Components.First();
        if (TryComp<FollowerComponent>(player.AttachedEntity, out var followerComponent))
        {
            GhostRoleComponent? prev = null;

            foreach (var current in data.Components)
            {
                if (prev?.Owner == followerComponent.Following)
                {
                    roleToFollow = current;
                    break;
                }

                prev = current;
            }
        }

        _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, roleToFollow.Owner);
    }

    /// <summary>
    ///     Attempts to perform a takeover for a player against the first entity found
    ///     in a group of ghost roles.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="roleIdentifier"></param>
    /// <returns></returns>
    public bool RequestTakeover(IPlayerSession player, string roleIdentifier)
    {
        if (!_ghostRoleData.TryGetValue(roleIdentifier, out var data))
            return false;

        if (data.Components.Count == 0)
            return false;

        // TODO: Probably broken for mixed lottery/takeover groups.
        var role = data.Components.First();
        return !role.RoleUseLottery && RequestTakeover(player, role);
    }

    /// <summary>
    ///     Attempts to perform a takeover for a player against a specific ghost role.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    public bool RequestTakeover(IPlayerSession player, GhostRoleComponent role)
    {
        if (!role.Take(player))
            return false; // Currently only fails if the role is already taken.

        _ghostRoleLotterySystem.ClearPlayerLotteryRequests(player);
        _ghostRoleLotterySystem.GhostRoleRemoveComponent(role);
        OnPlayerTakeoverComplete(player, role.RoleName);
        return true;
    }
}

/// <summary>
///     Raised when a system needs a ghost role component to be taken over by a player.
///     TODO: Method events bad. ES methods good.
/// </summary>
public sealed class GhostRoleRequestTakeoverMessage : EntityEventArgs
{
    /// <summary>
    ///     Player to takeover the entity.
    ///     Input parameter.
    /// </summary>
    public IPlayerSession Player { get; }

    /// <summary>
    ///     Ghost role to attempt to take over.
    ///     Input parameter.
    /// </summary>
    public GhostRoleComponent GhostRole { get; }

    /// <summary>
    ///     If the player successfully took over the entity.
    ///     Output parameter.
    /// </summary>
    public bool Result { get; set; }

    /// <summary>
    ///     If the role was consumed when being taken over. Certain ghost role types
    ///     can be used multiple times.
    /// </summary>
    public bool GhostRoleTaken { get; set; }

    public GhostRoleRequestTakeoverMessage(IPlayerSession player, GhostRoleComponent ghostRole)
    {
        Player = player;
        GhostRole = ghostRole;
    }
}



