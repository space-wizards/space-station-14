using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Mind;

public sealed class MindSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    // This is dictionary is required to track the minds of disconnected players that may have had their entity deleted.
    private readonly Dictionary<NetUserId, Mind> _userMinds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MindContainerComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<MindContainerComponent, EntityTerminatingEvent>(OnMindContainerTerminating);
        SubscribeLocalEvent<VisitingMindComponent, EntityTerminatingEvent>(OnVisitingTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnReset);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        WipeAllMinds();
    }

    public void SetGhostOnShutdown(EntityUid uid, bool value, MindContainerComponent? mind = null)
    {
        if (!Resolve(uid, ref mind))
            return;

        mind.GhostOnShutdown = value;
    }

    private void OnReset(RoundRestartCleanupEvent ev)
    {
        WipeAllMinds();
    }

    public void WipeAllMinds()
    {
        foreach (var mind in _userMinds.Values)
        {
            WipeMind(mind);
        }
        DebugTools.Assert(_userMinds.Count == 0);

        foreach (var unCastData in _playerManager.GetAllPlayerData())
        {
            if (unCastData.ContentData()?.Mind is not { } mind)
                continue;

            Log.Error("Player mind was missing from MindSystem dictionary.");
            WipeMind(mind);
        }
    }

    public Mind? GetMind(NetUserId user)
    {
        TryGetMind(user, out var mind);
        return mind;
    }

    public bool TryGetMind(NetUserId user, [NotNullWhen(true)] out Mind? mind)
    {
        if (_userMinds.TryGetValue(user, out mind))
        {
            DebugTools.Assert(mind.UserId == user);
            DebugTools.Assert(_playerManager.GetPlayerData(user).ContentData() is not {} data
                              || data.Mind == mind);
            return true;
        }

        DebugTools.Assert(_playerManager.GetPlayerData(user).ContentData()?.Mind == null);
        return false;
    }

    /// <summary>
    ///     Don't call this unless you know what the hell you're doing.
    ///     Use <see cref="MindSystem.TransferTo(Mind,System.Nullable{Robust.Shared.GameObjects.EntityUid},bool)"/> instead.
    ///     If that doesn't cover it, make something to cover it.
    /// </summary>
    private void InternalAssignMind(EntityUid uid, Mind value, MindContainerComponent? mind = null)
    {
        if (!Resolve(uid, ref mind))
            return;

        mind.Mind = value;
        RaiseLocalEvent(uid, new MindAddedMessage(), true);
    }

    /// <summary>
    ///     Don't call this unless you know what the hell you're doing.
    ///     Use <see cref="MindSystem.TransferTo(Mind,System.Nullable{Robust.Shared.GameObjects.EntityUid},bool)"/> instead.
    ///     If that doesn't cover it, make something to cover it.
    /// </summary>
    private void InternalEjectMind(EntityUid uid, MindContainerComponent? mind = null)
    {
        if (!Resolve(uid, ref mind, false))
            return;

        RaiseLocalEvent(uid, new MindRemovedMessage(), true);
        mind.Mind = null;
    }

    private void OnVisitingTerminating(EntityUid uid, VisitingMindComponent component, ref EntityTerminatingEvent args)
    {
        if (component.Mind != null)
            UnVisit(component.Mind);
    }

    private void OnMindContainerTerminating(EntityUid uid, MindContainerComponent component, ref EntityTerminatingEvent args)
    {
        // Let's not create ghosts if not in the middle of the round.
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        if (component.Mind is not { } mind)
            return;

        // If the player is currently visiting some other entity, simply attach to that entity.
        if (mind.VisitingEntity is {Valid: true} visiting
            && visiting != uid
            && !Deleted(visiting)
            && !Terminating(visiting))
        {
            TransferTo(mind, visiting);
            if (TryComp(visiting, out GhostComponent? ghost))
                _ghostSystem.SetCanReturnToBody(ghost, false);
            return;
        }

        TransferTo(mind, null);

        if (component.GhostOnShutdown && mind.Session != null)
        {
            var xform = Transform(uid);
            var gridId = xform.GridUid;
            var spawnPosition = Transform(uid).Coordinates;

            // Use a regular timer here because the entity has probably been deleted.
            Timer.Spawn(0, () =>
            {
                // Make extra sure the round didn't end between spawning the timer and it being executed.
                if (_gameTicker.RunLevel != GameRunLevel.InRound)
                    return;

                // Async this so that we don't throw if the grid we're on is being deleted.
                if (!_mapManager.GridExists(gridId))
                    spawnPosition = _gameTicker.GetObserverSpawnPoint();

                // TODO refactor observer spawning.
                // please.
                if (!spawnPosition.IsValid(EntityManager))
                {
                    // This should be an error, if it didn't cause tests to start erroring when they delete a player.
                    Log.Warning($"Entity \"{ToPrettyString(uid)}\" for {mind.CharacterName} was deleted, and no applicable spawn location is available.");
                    TransferTo(mind, null);
                    return;
                }

                var ghost = Spawn("MobObserver", spawnPosition);
                var ghostComponent = Comp<GhostComponent>(ghost);
                _ghostSystem.SetCanReturnToBody(ghostComponent, false);

                // Log these to make sure they're not causing the GameTicker round restart bugs...
                Log.Debug($"Entity \"{ToPrettyString(uid)}\" for {mind.CharacterName} was deleted, spawned \"{ToPrettyString(ghost)}\".");

                var val = mind.CharacterName ?? string.Empty;
                MetaData(ghost).EntityName = val;
                TransferTo(mind, ghost);
            });
        }
    }

    private void OnExamined(EntityUid uid, MindContainerComponent mindContainer, ExaminedEvent args)
    {
        if (!mindContainer.ShowExamineInfo || !args.IsInDetailsRange)
            return;

        var dead = _mobStateSystem.IsDead(uid);
        var hasSession = mindContainer.Mind?.Session;

        if (dead && hasSession == null)
            args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-dead-and-ssd", ("ent", uid))}[/color]");
        else if (dead)
            args.PushMarkup($"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", uid))}[/color]");
        else if (!mindContainer.HasMind)
            args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", uid))}[/color]");
        else if (hasSession == null)
            args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", uid))}[/color]");
    }

    private void OnSuicide(EntityUid uid, MindContainerComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (component.HasMind && component.Mind.PreventSuicide)
        {
            args.BlockSuicideAttempt(true);
        }
    }

    public Mind? GetMind(EntityUid uid, MindContainerComponent? mind = null)
    {
        if (!Resolve(uid, ref mind))
            return null;

        if (mind.HasMind)
            return mind.Mind;
        return null;
    }

    public Mind CreateMind(NetUserId? userId, string? name = null)
    {
        var mind = new Mind();
        mind.CharacterName = name;
        SetUserId(mind, userId);

        return mind;
    }

    /// <summary>
    ///     True if the OwnedEntity of this mind is physically dead.
    ///     This specific definition, as opposed to CharacterDeadIC, is used to determine if ghosting should allow return.
    /// </summary>
    public bool IsCharacterDeadPhysically(Mind mind)
    {
        // This is written explicitly so that the logic can be understood.
        // But it's also weird and potentially situational.
        // Specific considerations when updating this:
        //  + Does being turned into a borg (if/when implemented) count as dead?
        //    *If not, add specific conditions to users of this property where applicable.*
        //  + Is being transformed into a donut 'dead'?
        //    TODO: Consider changing the way ghost roles work.
        //    Mind is an *IC* mind, therefore ghost takeover is IC revival right now.
        //  + Is it necessary to have a reference to a specific 'mind iteration' to cycle when certain events happen?
        //    (If being a borg or AI counts as dead, then this is highly likely, as it's still the same Mind for practical purposes.)

        if (mind.OwnedEntity == null)
            return true;

        // This can be null if they're deleted (spike / brain nom)
        var targetMobState = EntityManager.GetComponentOrNull<MobStateComponent>(mind.OwnedEntity);
        // This can be null if it's a brain (this happens very often)
        // Brains are the result of gibbing so should definitely count as dead
        if (targetMobState == null)
            return true;
        // They might actually be alive.
        return _mobStateSystem.IsDead(mind.OwnedEntity.Value, targetMobState);
    }

    public void Visit(Mind mind, EntityUid entity)
    {
        if (mind.VisitingEntity != null)
        {
            Log.Error($"Attempted to visit an entity ({ToPrettyString(entity)}) while already visiting another ({ToPrettyString(mind.VisitingEntity.Value)}).");
            return;
        }

        if (HasComp<VisitingMindComponent>(entity))
        {
            Log.Error($"Attempted to visit an entity that already has a visiting mind. Entity: {ToPrettyString(entity)}");
            return;
        }

        mind.Session?.AttachToEntity(entity);
        mind.VisitingEntity = entity;

        // EnsureComp instead of AddComp to deal with deferred deletions.
        var comp = EnsureComp<VisitingMindComponent>(entity);
        comp.Mind = mind;
        Log.Info($"Session {mind.Session?.Name} visiting entity {entity}.");
    }

    /// <summary>
    /// Returns the mind to its original entity.
    /// </summary>
    public void UnVisit(Mind? mind)
    {
        if (mind == null || mind.VisitingEntity == null)
            return;

        RemoveVisitingEntity(mind);

        if (mind.Session == null || mind.Session.AttachedEntity == mind.VisitingEntity)
            return;

        var owned = mind.OwnedEntity;
        mind.Session.AttachToEntity(owned);

        if (owned.HasValue)
        {
            _adminLogger.Add(LogType.Mind, LogImpact.Low,
                $"{mind.Session.Name} returned to {ToPrettyString(owned.Value)}");
        }
    }

    /// <summary>
    /// Cleans up the VisitingEntity.
    /// </summary>
    /// <param name="mind"></param>
    private void RemoveVisitingEntity(Mind mind)
    {
        if (mind.VisitingEntity == null)
            return;

        var oldVisitingEnt = mind.VisitingEntity.Value;
        // Null this before removing the component to avoid any infinite loops.
        mind.VisitingEntity = null;

        if (TryComp(oldVisitingEnt, out VisitingMindComponent? visitComp))
        {
            visitComp.Mind = null;
            RemCompDeferred(oldVisitingEnt, visitComp);
        }

        RaiseLocalEvent(oldVisitingEnt, new MindUnvisitedMessage(), true);
    }

    public void WipeMind(IPlayerSession player)
    {
        var mind = player.ContentData()?.Mind;
        DebugTools.Assert(GetMind(player.UserId) == mind);
        WipeMind(mind);
    }

    /// <summary>
    /// Detaches a mind from all entities and clears the user ID.
    /// </summary>
    public void WipeMind(Mind? mind)
    {
        if (mind == null)
            return;

        TransferTo(mind, null);
        SetUserId(mind, null);
    }

    /// <summary>
    ///     Transfer this mind's control over to a new entity.
    /// </summary>
    /// <param name="mind">The mind to transfer</param>
    /// <param name="entity">
    ///     The entity to control.
    ///     Can be null, in which case it will simply detach the mind from any entity.
    /// </param>
    /// <param name="ghostCheckOverride">
    ///     If true, skips ghost check for Visiting Entity
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="entity"/> is already owned by another mind.
    /// </exception>
    public void TransferTo(Mind mind, EntityUid? entity, bool ghostCheckOverride = false)
    {
        if (entity == mind.OwnedEntity)
            return;

        MindContainerComponent? component = null;
        var alreadyAttached = false;

        if (entity != null)
        {
            component = EnsureComp<MindContainerComponent>(entity.Value);

            if (component.HasMind)
                _gameTicker.OnGhostAttempt(component.Mind, false);

            if (TryComp<ActorComponent>(entity.Value, out var actor))
            {
                // Happens when transferring to your currently visited entity.
                if (actor.PlayerSession != mind.Session)
                {
                    throw new ArgumentException("Visit target already has a session.", nameof(entity));
                }

                alreadyAttached = true;
            }
        }

        var oldComp = mind.OwnedComponent;
        var oldEntity = mind.OwnedEntity;
        if(oldComp != null && oldEntity != null)
            InternalEjectMind(oldEntity.Value, oldComp);

        SetOwnedEntity(mind, entity, component);
        if (mind.OwnedComponent != null){
            InternalAssignMind(mind.OwnedEntity!.Value, mind, mind.OwnedComponent);
            mind.OriginalOwnedEntity ??= mind.OwnedEntity;
        }

        // Don't do the full deletion cleanup if we're transferring to our VisitingEntity
        if (alreadyAttached)
        {
            // Set VisitingEntity null first so the removal of VisitingMind doesn't get through Unvisit() and delete what we're visiting.
            // Yes this control flow sucks.
            mind.VisitingEntity = null;
            RemComp<VisitingMindComponent>(entity!.Value);
        }
        else if (mind.VisitingEntity != null
              && (ghostCheckOverride // to force mind transfer, for example from ControlMobVerb
                  || !TryComp(mind.VisitingEntity!, out GhostComponent? ghostComponent) // visiting entity is not a Ghost
                  || !ghostComponent.CanReturnToBody))  // it is a ghost, but cannot return to body anyway, so it's okay
        {
            RemoveVisitingEntity(mind);
        }

        // Player is CURRENTLY connected.
        if (mind.Session != null && !alreadyAttached && mind.VisitingEntity == null)
        {
            mind.Session.AttachToEntity(entity);
            Log.Info($"Session {mind.Session.Name} transferred to entity {entity}.");
        }
    }

    /// <summary>
    /// Adds an objective to this mind.
    /// </summary>
    public bool TryAddObjective(Mind mind, ObjectivePrototype objectivePrototype)
    {
        if (!objectivePrototype.CanBeAssigned(mind))
            return false;
        var objective = objectivePrototype.GetObjective(mind);
        if (mind.Objectives.Contains(objective))
            return false;

        foreach (var condition in objective.Conditions)
        {
            _adminLogger.Add(LogType.Mind, LogImpact.Low, $"'{condition.Title}' added to mind of {MindOwnerLoggingString(mind)}");
        }


        mind.Objectives.Add(objective);
        return true;
    }

    /// <summary>
    /// Removes an objective to this mind.
    /// </summary>
    /// <returns>Returns true if the removal succeeded.</returns>
    public bool TryRemoveObjective(Mind mind, int index)
    {
        if (index < 0 || index >= mind.Objectives.Count) return false;

        var objective = mind.Objectives[index];

        foreach (var condition in objective.Conditions)
        {
            _adminLogger.Add(LogType.Mind, LogImpact.Low, $"'{condition.Title}' removed from the mind of {MindOwnerLoggingString(mind)}");
        }

        mind.Objectives.Remove(objective);
        return true;
    }

    /// <summary>
    ///     Gives this mind a new role.
    /// </summary>
    /// <param name="mind">The mind to add the role to.</param>
    /// <param name="role">The type of the role to give.</param>
    /// <returns>The instance of the role.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if we already have a role with this type.
    /// </exception>
    public void AddRole(Mind mind, Role role)
    {
        if (mind.Roles.Contains(role))
        {
            throw new ArgumentException($"We already have this role: {role}");
        }

        mind.Roles.Add(role);
        role.Greet();

        var message = new RoleAddedEvent(mind, role);
        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }

        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"'{role.Name}' added to mind of {MindOwnerLoggingString(mind)}");
    }

    /// <summary>
    ///     Removes a role from this mind.
    /// </summary>
    /// <param name="mind">The mind to remove the role from.</param>
    /// <param name="role">The type of the role to remove.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if we do not have this role.
    /// </exception>
    public void RemoveRole(Mind mind, Role role)
    {
        if (!mind.Roles.Contains(role))
        {
            throw new ArgumentException($"We do not have this role: {role}");
        }

        mind.Roles.Remove(role);

        var message = new RoleRemovedEvent(mind, role);

        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }
        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"'{role.Name}' removed from mind of {MindOwnerLoggingString(mind)}");
    }

    public bool HasRole<T>(Mind mind) where T : Role
    {
        return mind.Roles.Any(role => role is T);
    }

    public bool TryGetSession(Mind mind, [NotNullWhen(true)] out IPlayerSession? session)
    {
        return (session = mind.Session) != null;
    }

    /// <summary>
    /// Gets a mind from uid and/or MindContainerComponent. Used for null checks.
    /// </summary>
    /// <param name="uid">Entity UID that owns the mind.</param>
    /// <param name="mind">The returned mind.</param>
    /// <param name="mindContainerComponent">Mind component on <paramref name="uid"/> to get the mind from.</param>
    /// <returns>True if mind found. False if not.</returns>
    public bool TryGetMind(EntityUid uid, [NotNullWhen(true)] out Mind? mind, MindContainerComponent? mindContainerComponent = null)
    {
        mind = null;
        if (!Resolve(uid, ref mindContainerComponent))
            return false;

        if (!mindContainerComponent.HasMind)
            return false;

        mind = mindContainerComponent.Mind;
        return true;
    }

    /// <summary>
    /// Sets the Mind's OwnedComponent and OwnedEntity
    /// </summary>
    /// <param name="mind">Mind to set OwnedComponent and OwnedEntity on</param>
    /// <param name="uid">Entity owned by <paramref name="mind"/></param>
    /// <param name="mindContainerComponent">MindContainerComponent owned by <paramref name="mind"/></param>
    private void SetOwnedEntity(Mind mind, EntityUid? uid, MindContainerComponent? mindContainerComponent)
    {
        if (uid != null)
            Resolve(uid.Value, ref mindContainerComponent);

        mind.OwnedEntity = uid;
        mind.OwnedComponent = mindContainerComponent;
    }

    /// <summary>
    /// Sets the Mind's UserId, Session, and updates the player's PlayerData.
    /// This should have no direct effect on the entity that any mind is connected to, but it may change a player's attached entity.
    /// </summary>
    /// <param name="mind"></param>
    /// <param name="userId"></param>
    public void SetUserId(Mind mind, NetUserId? userId)
    {
        if (mind.UserId == userId)
            return;

        if (userId != null && !_playerManager.TryGetPlayerData(userId.Value, out _))
        {
            Log.Error($"Attempted to set mind user to invalid value {userId}");
            return;
        }

        if (mind.Session != null)
        {
            mind.Session.AttachToEntity(null);
            mind.Session = null;
        }

        if (mind.UserId != null)
        {
            _userMinds.Remove(mind.UserId.Value);
            if (_playerManager.GetPlayerData(mind.UserId.Value).ContentData() is { } oldData)
                oldData.Mind = null;
            mind.UserId = null;
        }

        if (userId == null)
        {
            DebugTools.AssertNull(mind.Session);
            return;
        }

        if (_userMinds.TryGetValue(userId.Value, out var oldMind))
            SetUserId(oldMind, null);

        DebugTools.AssertNull(_playerManager.GetPlayerData(userId.Value).ContentData()?.Mind);

        _userMinds[userId.Value] = mind;
        mind.UserId = userId;
        mind.OriginalOwnerUserId ??= userId;

        _playerManager.TryGetSessionById(userId.Value, out var ret);
        mind.Session = ret;

        // session may be null, but user data may still exist for disconnected players.
        if (_playerManager.GetPlayerData(userId.Value).ContentData() is { } data)
            data.Mind = mind;
    }

    /// <summary>
    ///     True if this Mind is 'sufficiently dead' IC (Objectives, EndText).
    ///     Note that this is *IC logic*, it's not necessarily tied to any specific truth.
    ///     "If administrators decide that zombies are dead, this returns true for zombies."
    ///     (Maybe you were looking for the action blocker system?)
    /// </summary>
    public bool IsCharacterDeadIc(Mind mind)
    {
        return IsCharacterDeadPhysically(mind);
    }

    /// <summary>
    ///     A string to represent the mind for logging
    /// </summary>
    private string MindOwnerLoggingString(Mind mind)
    {
        if (mind.OwnedEntity != null)
            return ToPrettyString(mind.OwnedEntity.Value);
        if (mind.UserId != null)
            return mind.UserId.Value.ToString();
        return "(originally " + mind.OriginalOwnerUserId + ")";
    }
}
