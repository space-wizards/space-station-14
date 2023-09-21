using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Mind;

public sealed class MindSystem : SharedMindSystem
{
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IMapManager _maps = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedGhostSystem _ghosts = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, EntityTerminatingEvent>(OnMindContainerTerminating);
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
                _ghosts.SetCanReturnToBody(ghost, false);
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
                if (!_maps.GridExists(gridId))
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
                _ghosts.SetCanReturnToBody(ghostComponent, false);

                // Log these to make sure they're not causing the GameTicker round restart bugs...
                Log.Debug($"Entity \"{ToPrettyString(uid)}\" for {mind.CharacterName} was deleted, spawned \"{ToPrettyString(ghost)}\".");

                var val = mind.CharacterName ?? string.Empty;
                _metaData.SetEntityName(ghost, val);
                TransferTo(mindId, ghost, mind: mind);
            });
        }
    }

    public override bool TryGetMind(NetUserId user, [NotNullWhen(true)] out EntityUid? mindId, [NotNullWhen(true)] out MindComponent? mind)
    {
        if (base.TryGetMind(user, out mindId, out mind))
        {
            DebugTools.Assert(_players.GetPlayerData(user).ContentData() is not { } data || data.Mind == mindId);
            return true;
        }

        DebugTools.Assert(_players.GetPlayerData(user).ContentData()?.Mind == null);
        return false;
    }

    public bool TryGetSession(EntityUid? mindId, [NotNullWhen(true)] out IPlayerSession? session)
    {
        session = null;
        return TryComp(mindId, out MindComponent? mind) && (session = (IPlayerSession?) mind.Session) != null;
    }

    public IPlayerSession? GetSession(MindComponent mind)
    {
        return (IPlayerSession?) mind.Session;
    }

    public bool TryGetSession(MindComponent mind, [NotNullWhen(true)] out IPlayerSession? session)
    {
        return (session = GetSession(mind)) != null;
    }

    public override void WipeAllMinds()
    {
        base.WipeAllMinds();

        foreach (var unCastData in _players.GetAllPlayerData())
        {
            if (unCastData.ContentData()?.Mind is not { } mind)
                continue;

            Log.Error("Player mind was missing from MindSystem dictionary.");
            WipeMind(mind);
        }
    }

    public override void Visit(EntityUid mindId, EntityUid entity, MindComponent? mind = null)
    {
        base.Visit(mindId, entity, mind);

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

        GetSession(mind)?.AttachToEntity(entity);
        mind.VisitingEntity = entity;

        // EnsureComp instead of AddComp to deal with deferred deletions.
        var comp = EnsureComp<VisitingMindComponent>(entity);
        comp.MindId = mindId;
        Log.Info($"Session {mind.Session?.Name} visiting entity {entity}.");
    }

    public override void UnVisit(EntityUid mindId, MindComponent? mind = null)
    {
        base.UnVisit(mindId, mind);

        if (!Resolve(mindId, ref mind))
            return;

        if (mind.VisitingEntity == null)
            return;

        RemoveVisitingEntity(mind);

        if (mind.Session == null || mind.Session.AttachedEntity == mind.VisitingEntity)
            return;

        var owned = mind.OwnedEntity;
        GetSession(mind)?.AttachToEntity(owned);

        if (owned.HasValue)
        {
            _adminLogger.Add(LogType.Mind, LogImpact.Low,
                $"{mind.Session.Name} returned to {ToPrettyString(owned.Value)}");
        }
    }

    public override void TransferTo(EntityUid mindId, EntityUid? entity, bool ghostCheckOverride = false, bool createGhost = true,
        MindComponent? mind = null)
    {
        base.TransferTo(mindId, entity, ghostCheckOverride, createGhost, mind);

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
            _ghosts.SetCanReturnToBody(ghostComponent, false);
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
        var session = GetSession(mind);
        if (session != null && !alreadyAttached && mind.VisitingEntity == null)
        {
            _actor.Attach(entity, session, true);
            Log.Info($"Session {session.Name} transferred to entity {entity}.");
        }

        if (mind.OwnedComponent != null)
        {
            mind.OwnedComponent.Mind = mindId;
            RaiseLocalEvent(mind.OwnedEntity!.Value, new MindAddedMessage(), true);
            mind.OriginalOwnedEntity ??= mind.OwnedEntity;
        }
    }

    /// <summary>
    /// Sets the Mind's UserId, Session, and updates the player's PlayerData. This should have no direct effect on the
    /// entity that any mind is connected to, except as a side effect of the fact that it may change a player's
    /// attached entity. E.g., ghosts get deleted.
    /// </summary>
    public override void SetUserId(EntityUid mindId, NetUserId? userId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

        if (mind.UserId == userId)
            return;

        if (userId != null && !_players.TryGetPlayerData(userId.Value, out _))
        {
            Log.Error($"Attempted to set mind user to invalid value {userId}");
            return;
        }

        if (mind.Session != null)
        {
            _actor.Attach(null, GetSession(mind)!);
            mind.Session = null;
        }

        if (mind.UserId != null)
        {
            UserMinds.Remove(mind.UserId.Value);
            if (_players.GetPlayerData(mind.UserId.Value).ContentData() is { } oldData)
                oldData.Mind = null;
            mind.UserId = null;
        }

        if (userId == null)
        {
            DebugTools.AssertNull(mind.Session);
            return;
        }

        if (UserMinds.TryGetValue(userId.Value, out var oldMindId) &&
            TryComp(oldMindId, out MindComponent? oldMind))
        {
            SetUserId(oldMindId, null, oldMind);
        }

        DebugTools.AssertNull(_players.GetPlayerData(userId.Value).ContentData()?.Mind);

        UserMinds[userId.Value] = mindId;
        mind.UserId = userId;
        mind.OriginalOwnerUserId ??= userId;

        if (_players.TryGetSessionById(userId.Value, out var ret))
        {
            mind.Session = ret;
            _actor.Attach(mind.CurrentEntity, ret);
        }

        // session may be null, but user data may still exist for disconnected players.
        if (_players.GetPlayerData(userId.Value).ContentData() is { } data)
            data.Mind = mindId;
    }
}
