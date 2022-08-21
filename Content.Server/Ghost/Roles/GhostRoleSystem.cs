using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Database;
using Content.Shared.GameTicking;
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

    private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

    /// <summary>
    /// Lookup ghost role components by the role name. This includes unavailable ghost roles.
    /// </summary>
    private readonly Dictionary<string, HashSet<GhostRoleComponent>> _ghostRoleLookup = new ();

    [ViewVariables]
    private IReadOnlyList<GhostRoleComponent> GhostRoles => EntityQuery<GhostRoleComponent>().ToList();


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
        var prevAvailable = component.Available;

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
                break;
            case DamageState.Invalid:
            default:
                break;
        }

        if(prevAvailable != component.Available)
            RaiseLocalEvent(uid, new GhostRoleAvailabilityChangedEvent(component.Owner, component, component.Available), true);
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
        var prevAvailable = component.Available;
        component.Taken = true; // Handle take-overs outside of this system (e.g. Admin take-over).

        if(prevAvailable == component.Available)
            RaiseLocalEvent(uid, new GhostRoleAvailabilityChangedEvent(uid, component, component.Available), true);
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
        if (!_ghostRoleLookup.TryGetValue(component._InternalRoleName, out var components))
            _ghostRoleLookup[component._InternalRoleName] = components = new HashSet<GhostRoleComponent>();

        components.Add(component);

        if (component._InternalRoleRules == "")
            component._InternalRoleRules = Loc.GetString("ghost-role-component-default-rules");
    }

    private void OnShutdown(EntityUid uid, GhostRoleComponent component, ComponentShutdown args)
    {
        _ghostRoleLookup.GetValueOrDefault(component._InternalRoleName)?.Remove(component);

        if(!(component.Taken || component.Damaged || component.RoleGroupReserved))
            RaiseLocalEvent(uid, new GhostRoleAvailabilityChangedEvent(uid, component, false), true);
    }

    private void OnGhostRoleGroupEntityAttached(EntityUid uid, GhostRoleComponent component, GhostRoleGroupEntityAttachedEvent args)
    {
        var prevAvailable = component.Available;

        // Taken by a ghost role group. Mark as unavailable.
        component.RoleGroupReserved = true;

        if(component.Available != prevAvailable)
            RaiseLocalEvent(uid, new GhostRoleAvailabilityChangedEvent(uid, component, component.Available), true);
    }

    private void OnGhostRoleGroupEntityDetached(EntityUid uid, GhostRoleComponent component, GhostRoleGroupEntityDetachedEvent args)
    {
        var prevAvailable = component.Available;
        component.RoleGroupReserved = false;

        if(component.Available != prevAvailable)
            RaiseLocalEvent(uid, new GhostRoleAvailabilityChangedEvent(uid, component, component.Available), true);
    }

    /// <summary>
    /// Retrieves ghost roles that are able to be taken over.
    /// </summary>
    public IEnumerable<GhostRoleComponent> GetAvailableGhostRoles()
    {
        return EntityQuery<GhostRoleComponent>()
            .Where(comp => comp.Available);
    }

    public void OnPlayerTakeoverComplete(IPlayerSession player, GhostRoleComponent comp)
    {
        if (player.AttachedEntity == null)
            return;

        _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {comp.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");
    }

    /// <summary>
    ///     Attempts to perform a takeover for a player against a specific ghost role.
    /// </summary>
    /// <param name="player">The player performing the takeover.</param>
    /// <param name="role">The ghost role being taken over.</param>
    /// <returns></returns>
    public bool PerformTakeover(IPlayerSession player, GhostRoleComponent role)
    {
        var prevAvailable = role.Available; // This might be a role group takeover, etc.

        if (!role.Take(player))
            return false; // Currently only fails if the role is already taken.

        if(role.Taken && prevAvailable != role.Available)
            RaiseLocalEvent(role.Owner, new GhostRoleAvailabilityChangedEvent(role.Owner, role, role.Available), true);

        OnPlayerTakeoverComplete(player, role);
        return true;
    }

    #region Setters
    public void SetRoleName(GhostRoleComponent ghostRoleComponent, string value)
    {
        if (ghostRoleComponent._InternalRoleName == value)
            return;

        var previous = ghostRoleComponent._InternalRoleName;
        var previousRoleName = ghostRoleComponent.RoleName;

        ghostRoleComponent._InternalRoleName = value;

        // Move the role between lookups.
        _ghostRoleLookup.GetValueOrDefault(previous)?.Remove(ghostRoleComponent);
        if (!_ghostRoleLookup.TryGetValue(ghostRoleComponent._InternalRoleName, out var data))
            _ghostRoleLookup[ghostRoleComponent._InternalRoleName] = data = new HashSet<GhostRoleComponent>();

        data.Add(ghostRoleComponent);

        RaiseLocalEvent(ghostRoleComponent.Owner, new GhostRoleModifiedEvent(ghostRoleComponent)
        {
            PreviousRoleIdentifier = previous,
            PreviousRoleName = previousRoleName
        }, true);
    }

    public void SetRoleDescription(GhostRoleComponent ghostRoleComponent, string value)
    {
        if (ghostRoleComponent._InternalRoleDescription == value)
            return;

        var previous = ghostRoleComponent._InternalRoleDescription;

        ghostRoleComponent._InternalRoleDescription = value;
        RaiseLocalEvent(ghostRoleComponent.Owner, new GhostRoleModifiedEvent(ghostRoleComponent)
        {
            PreviousRoleDescription = previous
        }, true);
    }

    public void SetRoleRules(GhostRoleComponent ghostRoleComponent, string value)
    {
        if (ghostRoleComponent._InternalRoleRules == value)
            return;

        var previous = ghostRoleComponent._InternalRoleRules;

        ghostRoleComponent._InternalRoleRules = value;
        RaiseLocalEvent(ghostRoleComponent.Owner, new GhostRoleModifiedEvent(ghostRoleComponent)
        {
            PreviousRoleRule = previous
        }, true);
    }

    public void SetRoleLotteryEnabled(GhostRoleComponent ghostRoleComponent, bool value)
    {
        if (ghostRoleComponent._InternalRoleLotteryEnabled == value)
            return;

        var previous = ghostRoleComponent._InternalRoleLotteryEnabled;

        ghostRoleComponent._InternalRoleLotteryEnabled = value;
        RaiseLocalEvent(ghostRoleComponent.Owner, new GhostRoleModifiedEvent(ghostRoleComponent)
        {
            PreviousRoleLotteryEnabled = previous
        }, true);
    }
    #endregion
}



