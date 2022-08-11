using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.AI.Components;
using Content.Server.AI.EntitySystems;
using Content.Server.EUI;
using Content.Server.Ghost.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
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
    ///     Cache mapping the identifiers back to the components for quick lookup.
    /// </summary>
    private readonly Dictionary<string, GhostRoleData> _ghostRoleData = new ();

    // [ViewVariables]
    // public IReadOnlyCollection<GhostRoleComponent> GhostRoleEntries => _ghostRoles.Values;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GhostRoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnShutdown);
    }

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
            var prev = roleToFollow;

            foreach (var current in data.Components)
            {
                if (prev.Owner == player.AttachedEntity)
                {
                    roleToFollow = current;
                    break;
                }

                prev = current;
            }
        }

        _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, roleToFollow.Owner);
    }

    public bool RequestTakeover(IPlayerSession player, string roleIdentifier)
    {
        if (!_ghostRoleData.TryGetValue(roleIdentifier, out var data))
            return false;

        if (data.Components.Count == 0)
            return false;

        var role = data.Components.First();

        if (!role.Take(player))
            return false; // Currently only fails if the role is already taken.

        _ghostRoleLotterySystem.ClearPlayerLotteryRequests(player);
        _ghostRoleLotterySystem.GhostRoleRemoveComponent(role);
        return true;
    }

    private void OnMobStateChanged(EntityUid uid, GhostRoleComponent component, MobStateChangedEvent args)
    {
        switch (args.CurrentMobState)
        {
            case DamageState.Alive:
            {
                if (!component.Taken)
                    _ghostRoleLotterySystem.GhostRoleQueueComponent(component);
                break;
            }
            case DamageState.Critical:
            case DamageState.Dead:
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

    public GhostRoleInfo? GetGhostRolesInfoOrNull(string roleIdentifier)
    {
        if (!_ghostRoleData.TryGetValue(roleIdentifier, out var data))
            return null;

        var lotteryCount = 0;
        var takeoverCount = 0;

        foreach (var comp in data.Components)
        {
            var count = comp is GhostRoleMobSpawnerComponent spawn ? spawn.AvailableTakeovers : 1;

            if (comp.RoleUseLottery)
                lotteryCount += count;
            else
                takeoverCount += count;
        }

        var role = new GhostRoleInfo()
        {
            Identifier = roleIdentifier,
            Name = data.RoleName,
            Description = data.RoleDescription,
            Rules = data.RoleRules,
            IsRequested = false, // TODO: Fix this
            AvailableLotteryRoleCount = lotteryCount,
            AvailableImmediateRoleCount = takeoverCount,
        };

        return role;
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
        _ghostRoleLotterySystem.GhostRoleRemoveComponent(component);
    }

    private void OnMindRemoved(EntityUid uid, GhostRoleComponent component, MindRemovedMessage args)
    {
        // Avoid re-registering it for duplicate entries and potential exceptions.
        if (!component.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
            return;

        component.Taken = false;
        _ghostRoleLotterySystem.GhostRoleQueueComponent(component);
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

        if (!_ghostRoleData.TryGetValue(component.RoleName, out var data))
        {
            _ghostRoleData[component.RoleName] = data = new GhostRoleData()
            {
                RoleIdentifier = _ghostRoleLotterySystem.NextIdentifier,
                RoleName = component.RoleName,
                RoleDescription = component.RoleDescription,
                RoleRules = component.RoleRules,
                Components = new HashSet<GhostRoleComponent>(),
            };
        }

        component.Identifier = data.RoleIdentifier;

        data.Components.Add(component);

        if(component.RoleUseLottery)
            _ghostRoleLotterySystem.GhostRoleQueueComponent(component);
    }

    private void OnShutdown(EntityUid uid, GhostRoleComponent component, ComponentShutdown args)
    {
        _ghostRoleLotterySystem.GhostRoleRemoveComponent(component);
    }

    public void OnPlayerTakeoverComplete(IPlayerSession player, string roleIdentifier)
    {
        if (player.AttachedEntity == null || !_ghostRoleData.TryGetValue(roleIdentifier, out var data))
            return;

        _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {data.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");
    }

    public int RequestCountForRole(string ghostRoleIdentifier)
    {
        if (!_ghostRoleData.TryGetValue(ghostRoleIdentifier, out var data))
            return 0;

        var count = 0;
        foreach (var c in data.Components)
        {
            if (c is GhostRoleMobSpawnerComponent spawnerComponent)
                count += spawnerComponent.AvailableTakeovers;
            else
                count++;
        }

        return count;
    }
}

public sealed class GhostRolesChangedEventArgs
{
    /// <summary>
    ///     Indicates only this player session needs to be updated.
    /// </summary>
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

