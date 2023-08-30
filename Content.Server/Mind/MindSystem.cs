using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Players;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
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
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    // This is dictionary is required to track the minds of disconnected players that may have had their entity deleted.
    private readonly Dictionary<NetUserId, EntityUid> _userMinds = new();

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

    public EntityUid? GetMind(NetUserId user)
    {
        TryGetMind(user, out var mind, out _);
        return mind;
    }

    public bool TryGetMind(NetUserId user, [NotNullWhen(true)] out EntityUid? mindId, [NotNullWhen(true)] out MindComponent? mind)
    {
        if (_userMinds.TryGetValue(user, out var mindIdValue) &&
            TryComp(mindIdValue, out mind))
        {
            DebugTools.Assert(mind.UserId == user);
            DebugTools.Assert(_playerManager.GetPlayerData(user).ContentData() is not {} data
                              || data.Mind == mindIdValue);

            mindId = mindIdValue;
            return true;
        }

        DebugTools.Assert(_playerManager.GetPlayerData(user).ContentData()?.Mind == null);
        mindId = null;
        mind = null;
        return false;
    }

    private void OnVisitingTerminating(EntityUid uid, VisitingMindComponent component, ref EntityTerminatingEvent args)
    {
        if (component.MindId != null)
            UnVisit(component.MindId.Value);
    }

    private void OnMindContainerTerminating(EntityUid uid, MindContainerComponent component, ref EntityTerminatingEvent args)
    {
        // Let's not create ghosts if not in the middle of the round.
        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            return;

        if (!TryGetMind(uid, out var mindId, out var mind, component))
            return;

        // If the player is currently visiting some other entity, simply attach to that entity.
        if (mind.VisitingEntity is {Valid: true} visiting
            && visiting != uid
            && !Deleted(visiting)
            && !Terminating(visiting))
        {
            TransferTo(mindId, visiting, mind: mind);
            if (TryComp(visiting, out GhostComponent? ghost))
                _ghostSystem.SetCanReturnToBody(ghost, false);
            return;
        }

        TransferTo(mindId, null, createGhost: false, mind: mind);

        if (component.GhostOnShutdown && mind.Session != null)
        {
            var xform = Transform(uid);
            var gridId = xform.GridUid;
            var spawnPosition = Transform(uid).Coordinates;

            // Use a regular timer here because the entity has probably been deleted.
            Timer.Spawn(0, () =>
            {
                // Make extra sure the round didn't end between spawning the timer and it being executed.
                if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
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
                    TransferTo(mindId, null, createGhost: false, mind: mind);
                    return;
                }

                var ghost = Spawn("MobObserver", spawnPosition);
                var ghostComponent = Comp<GhostComponent>(ghost);
                _ghostSystem.SetCanReturnToBody(ghostComponent, false);

                // Log these to make sure they're not causing the GameTicker round restart bugs...
                Log.Debug($"Entity \"{ToPrettyString(uid)}\" for {mind.CharacterName} was deleted, spawned \"{ToPrettyString(ghost)}\".");

                var val = mind.CharacterName ?? string.Empty;
                _metaData.SetEntityName(ghost, val);
                TransferTo(mindId, ghost, mind: mind);
            });
        }
    }

    private void OnExamined(EntityUid uid, MindContainerComponent mindContainer, ExaminedEvent args)
    {
        if (!mindContainer.ShowExamineInfo || !args.IsInDetailsRange)
            return;

        var dead = _mobStateSystem.IsDead(uid);
        var hasSession = CompOrNull<MindComponent>(mindContainer.Mind)?.Session;

        if (dead && !mindContainer.HasMind)
            args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-dead-and-irrecoverable", ("ent", uid))}[/color]");
        else if (dead && hasSession == null)
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

        if (TryComp(component.Mind, out MindComponent? mind) && mind.PreventSuicide)
        {
            args.BlockSuicideAttempt(true);
        }
    }

    public EntityUid? GetMind(EntityUid uid, MindContainerComponent? mind = null)
    {
        if (!Resolve(uid, ref mind))
            return null;

        if (mind.HasMind)
            return mind.Mind;

        return null;
    }

    public EntityUid CreateMind(NetUserId? userId, string? name = null)
    {
        var mindId = Spawn(null, MapCoordinates.Nullspace);
        var mind = EnsureComp<MindComponent>(mindId);
        mind.CharacterName = name;
        SetUserId(mindId, userId, mind);

        Dirty(mindId, MetaData(mindId));

        return mindId;
    }

    /// <summary>
    ///     True if the OwnedEntity of this mind is physically dead.
    ///     This specific definition, as opposed to CharacterDeadIC, is used to determine if ghosting should allow return.
    /// </summary>
    public bool IsCharacterDeadPhysically(MindComponent mind)
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

    public void Visit(EntityUid mindId, EntityUid entity, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

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
        comp.MindId = mindId;
        Log.Info($"Session {mind.Session?.Name} visiting entity {entity}.");
    }

    /// <summary>
    /// Returns the mind to its original entity.
    /// </summary>
    public void UnVisit(EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

        if (mind.VisitingEntity == null)
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
    /// Returns the mind to its original entity.
    /// </summary>
    public void UnVisit(IPlayerSession? player)
    {
        if (player == null || !TryGetMind(player, out var mindId, out var mind))
            return;

        UnVisit(mindId, mind);
    }

    /// <summary>
    /// Cleans up the VisitingEntity.
    /// </summary>
    /// <param name="mind"></param>
    private void RemoveVisitingEntity(MindComponent mind)
    {
        if (mind.VisitingEntity == null)
            return;

        var oldVisitingEnt = mind.VisitingEntity.Value;
        // Null this before removing the component to avoid any infinite loops.
        mind.VisitingEntity = null;

        if (TryComp(oldVisitingEnt, out VisitingMindComponent? visitComp))
        {
            visitComp.MindId = null;
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
    public void WipeMind(EntityUid? mindId, MindComponent? mind = null)
    {
        if (mindId == null || !Resolve(mindId.Value, ref mind, false))
            return;

        TransferTo(mindId.Value, null, mind: mind);
        SetUserId(mindId.Value, null, mind: mind);
    }

    /// <summary>
    ///     Transfer this mind's control over to a new entity.
    /// </summary>
    /// <param name="mindId">The mind to transfer</param>
    /// <param name="entity">
    ///     The entity to control.
    ///     Can be null, in which case it will simply detach the mind from any entity.
    /// </param>
    /// <param name="ghostCheckOverride">
    ///     If true, skips ghost check for Visiting Entity
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="entity"/> is already controlled by another player.
    /// </exception>
    public void TransferTo(EntityUid mindId, EntityUid? entity, bool ghostCheckOverride = false, bool createGhost = true, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

        if (entity == mind.OwnedEntity)
            return;

        MindContainerComponent? component = null;
        var alreadyAttached = false;

        if (entity != null)
        {
            component = EnsureComp<MindContainerComponent>(entity.Value);

            if (component.HasMind)
                _gameTicker.OnGhostAttempt(component.Mind.Value, false);

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
        else if (createGhost)
        {
            var position = Deleted(mind.OwnedEntity)
                ? _gameTicker.GetObserverSpawnPoint().ToMap(EntityManager, _transform)
                : Transform(mind.OwnedEntity.Value).MapPosition;

            entity = Spawn("MobObserver", position);
            var ghostComponent = Comp<GhostComponent>(entity.Value);
            _ghostSystem.SetCanReturnToBody(ghostComponent, false);
        }

        var oldComp = mind.OwnedComponent;
        var oldEntity = mind.OwnedEntity;
        if (oldComp != null && oldEntity != null)
        {
            oldComp.Mind = null;
            RaiseLocalEvent(oldEntity.Value, new MindRemovedMessage(oldEntity.Value, mind), true);
        }

        SetOwnedEntity(mind, entity, component);

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
            _actor.Attach(entity, mind.Session, true);
            Log.Info($"Session {mind.Session.Name} transferred to entity {entity}.");
        }

        if (mind.OwnedComponent != null)
        {
            mind.OwnedComponent.Mind = mindId;
            RaiseLocalEvent(mind.OwnedEntity!.Value, new MindAddedMessage(), true);
            mind.OriginalOwnedEntity ??= mind.OwnedEntity;
        }
    }

    /// <summary>
    /// Adds an objective to this mind.
    /// </summary>
    public bool TryAddObjective(EntityUid mindId, MindComponent mind, ObjectivePrototype objectivePrototype)
    {
        if (!objectivePrototype.CanBeAssigned(mindId, mind))
            return false;
        var objective = objectivePrototype.GetObjective(mindId, mind);
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
    public bool TryRemoveObjective(MindComponent mind, int index)
    {
        if (index < 0 || index >= mind.Objectives.Count)
            return false;

        var objective = mind.Objectives[index];

        foreach (var condition in objective.Conditions)
        {
            _adminLogger.Add(LogType.Mind, LogImpact.Low, $"'{condition.Title}' removed from the mind of {MindOwnerLoggingString(mind)}");
        }

        mind.Objectives.Remove(objective);
        return true;
    }

    public bool TryGetSession(EntityUid? mindId, [NotNullWhen(true)] out IPlayerSession? session)
    {
        session = null;
        return TryComp(mindId, out MindComponent? mind) && (session = mind.Session) != null;
    }

    /// <summary>
    /// Gets a mind from uid and/or MindContainerComponent. Used for null checks.
    /// </summary>
    /// <param name="uid">Entity UID that owns the mind.</param>
    /// <param name="mindId">The mind id.</param>
    /// <param name="mind">The returned mind.</param>
    /// <param name="container">Mind component on <paramref name="uid"/> to get the mind from.</param>
    /// <returns>True if mind found. False if not.</returns>
    public bool TryGetMind(
        EntityUid uid,
        out EntityUid mindId,
        [NotNullWhen(true)] out MindComponent? mind,
        MindContainerComponent? container = null)
    {
        mindId = default;
        mind = null;

        if (!Resolve(uid, ref container, false))
            return false;

        if (!container.HasMind)
            return false;

        mindId = container.Mind ?? default;
        return TryComp(mindId, out mind);
    }

    public bool TryGetMind(
        PlayerData player,
        out EntityUid mindId,
        [NotNullWhen(true)] out MindComponent? mind)
    {
        mindId = player.Mind ?? default;
        return TryComp(mindId, out mind);
    }

    public bool TryGetMind(
        IPlayerSession? player,
        out EntityUid mindId,
        [NotNullWhen(true)] out MindComponent? mind)
    {
        mindId = default;
        mind = null;
        return player?.ContentData() is { } data && TryGetMind(data, out mindId, out mind);
    }

    /// <summary>
    /// Sets the Mind's OwnedComponent and OwnedEntity
    /// </summary>
    /// <param name="mind">Mind to set OwnedComponent and OwnedEntity on</param>
    /// <param name="uid">Entity owned by <paramref name="mind"/></param>
    /// <param name="mindContainerComponent">MindContainerComponent owned by <paramref name="mind"/></param>
    private void SetOwnedEntity(MindComponent mind, EntityUid? uid, MindContainerComponent? mindContainerComponent)
    {
        if (uid != null)
            Resolve(uid.Value, ref mindContainerComponent);

        mind.OwnedEntity = uid;
        mind.OwnedComponent = mindContainerComponent;
    }

    /// <summary>
    /// Sets the Mind's UserId, Session, and updates the player's PlayerData. This should have no direct effect on the
    /// entity that any mind is connected to, except as a side effect of the fact that it may change a player's
    /// attached entity. E.g., ghosts get deleted.
    /// </summary>
    public void SetUserId(EntityUid mindId, NetUserId? userId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

        if (mind.UserId == userId)
            return;

        if (userId != null && !_playerManager.TryGetPlayerData(userId.Value, out _))
        {
            Log.Error($"Attempted to set mind user to invalid value {userId}");
            return;
        }

        if (mind.Session != null)
        {
            _actor.Attach(null, mind.Session);
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

        if (_userMinds.TryGetValue(userId.Value, out var oldMindId) &&
            TryComp(oldMindId, out MindComponent? oldMind))
        {
            SetUserId(oldMindId, null, oldMind);
        }

        DebugTools.AssertNull(_playerManager.GetPlayerData(userId.Value).ContentData()?.Mind);

        _userMinds[userId.Value] = mindId;
        mind.UserId = userId;
        mind.OriginalOwnerUserId ??= userId;

        if (_playerManager.TryGetSessionById(userId.Value, out var ret))
        {
            mind.Session = ret;
            _actor.Attach(mind.CurrentEntity, mind.Session);
        }

        // session may be null, but user data may still exist for disconnected players.
        if (_playerManager.GetPlayerData(userId.Value).ContentData() is { } data)
            data.Mind = mindId;
    }

    /// <summary>
    ///     True if this Mind is 'sufficiently dead' IC (Objectives, EndText).
    ///     Note that this is *IC logic*, it's not necessarily tied to any specific truth.
    ///     "If administrators decide that zombies are dead, this returns true for zombies."
    ///     (Maybe you were looking for the action blocker system?)
    /// </summary>
    public bool IsCharacterDeadIc(MindComponent mind)
    {
        if (mind.OwnedEntity is { } owned)
        {
            var ev = new GetCharactedDeadIcEvent(null);
            RaiseLocalEvent(owned, ref ev);

            if (ev.Dead != null)
                return ev.Dead.Value;
        }

        return IsCharacterDeadPhysically(mind);
    }

    /// <summary>
    ///     A string to represent the mind for logging
    /// </summary>
    public string MindOwnerLoggingString(MindComponent mind)
    {
        if (mind.OwnedEntity != null)
            return ToPrettyString(mind.OwnedEntity.Value);
        if (mind.UserId != null)
            return mind.UserId.Value.ToString();
        return "(originally " + mind.OriginalOwnerUserId + ")";
    }

    public string? GetCharacterName(NetUserId userId)
    {
        return TryGetMind(userId, out _, out var mind) ? mind.CharacterName : null;
    }
}

/// <summary>
/// Raised on an entity to determine whether or not they are "dead" in IC-logic.
/// If not handled, then it will simply check if they are dead physically.
/// </summary>
/// <param name="Dead"></param>
[ByRefEvent]
public record struct GetCharactedDeadIcEvent(bool? Dead);
