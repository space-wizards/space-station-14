using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind.Commands;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Mind;

public sealed class MindSystem : SharedMindSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly GhostSystem _ghosts = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, EntityTerminatingEvent>(OnMindContainerTerminating);
        SubscribeLocalEvent<MindComponent, ComponentShutdown>(OnMindShutdown);
    }

    private void OnMindShutdown(EntityUid uid, MindComponent mind, ComponentShutdown args)
    {
        if (mind.UserId is {} user)
        {
            UserMinds.Remove(user);
            if (_players.TryGetPlayerData(user, out var data) && data.ContentData() is { } oldData)
                oldData.Mind = null;
            mind.UserId = null;
        }

        if (mind.OwnedEntity != null && !TerminatingOrDeleted(mind.OwnedEntity.Value))
            TransferTo(uid, null, mind: mind, createGhost: false);

        mind.OwnedEntity = null;
    }

    private void OnMindContainerTerminating(EntityUid uid, MindContainerComponent component, ref EntityTerminatingEvent args)
    {
        if (!TryGetMind(uid, out var mindId, out var mind, component))
            return;

        // If the player is currently visiting some other entity, simply attach to that entity.
        if (mind.VisitingEntity is {Valid: true} visiting
            && visiting != uid
            && !Deleted(visiting)
            && !Terminating(visiting))
        {
            TransferTo(mindId, visiting, mind: mind);
            if (TryComp(visiting, out GhostComponent? ghostComp))
                _ghosts.SetCanReturnToBody(ghostComp, false);
            return;
        }

        TransferTo(mindId, null, createGhost: false, mind: mind);
        DebugTools.AssertNull(mind.OwnedEntity);

        if (!component.GhostOnShutdown || mind.Session == null || _gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            return;

        var ghost = _ghosts.SpawnGhost((mindId, mind), uid);
        if (ghost != null)
            // Log these to make sure they're not causing the GameTicker round restart bugs...
            Log.Debug($"Entity \"{ToPrettyString(uid)}\" for {mind.CharacterName} was deleted, spawned \"{ToPrettyString(ghost)}\".");
        else
            // This should be an error, if it didn't cause tests to start erroring when they delete a player.
            Log.Warning($"Entity \"{ToPrettyString(uid)}\" for {mind.CharacterName} was deleted, and no applicable spawn location is available.");
    }

    public override bool TryGetMind(NetUserId user, [NotNullWhen(true)] out EntityUid? mindId, [NotNullWhen(true)] out MindComponent? mind)
    {
        if (base.TryGetMind(user, out mindId, out mind))
        {
            DebugTools.Assert(!_players.TryGetPlayerData(user, out var playerData) || playerData.ContentData() is not { } data || data.Mind == mindId);
            return true;
        }

        DebugTools.Assert(!_players.TryGetPlayerData(user, out var pData) || pData.ContentData()?.Mind == null);
        return false;
    }

    public ICommonSession? GetSession(MindComponent mind)
    {
        return mind.Session;
    }

    public bool TryGetSession(MindComponent mind, [NotNullWhen(true)] out ICommonSession? session)
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

        mind.VisitingEntity = entity;

        // EnsureComp instead of AddComp to deal with deferred deletions.
        var comp = EnsureComp<VisitingMindComponent>(entity);
        comp.MindId = mindId;

        // Do this AFTER the entity changes above as this will fire off a player-detached event
        // which will run ghosting twice.
        if (GetSession(mind) is { } session)
            _players.SetAttachedEntity(session, entity);

        Log.Info($"Session {mind.Session?.Name} visiting entity {entity}.");
    }

    public override void UnVisit(EntityUid mindId, MindComponent? mind = null)
    {
        base.UnVisit(mindId, mind);

        if (!Resolve(mindId, ref mind))
            return;

        if (mind.VisitingEntity == null)
            return;

        RemoveVisitingEntity(mindId, mind);

        if (mind.Session == null || mind.Session.AttachedEntity == mind.VisitingEntity)
            return;

        var owned = mind.OwnedEntity;
        if (GetSession(mind) is { } session)
            _players.SetAttachedEntity(session, owned);

        if (owned.HasValue)
        {
            _adminLogger.Add(LogType.Mind, LogImpact.Low,
                $"{mind.Session.Name} returned to {ToPrettyString(owned.Value)}");
        }
    }

    public override void TransferTo(EntityUid mindId, EntityUid? entity, bool ghostCheckOverride = false, bool createGhost = true,
        MindComponent? mind = null)
    {
        if (mind == null && !Resolve(mindId, ref mind))
            return;

        if (entity == mind.OwnedEntity)
            return;

        Dirty(mindId, mind);
        MindContainerComponent? component = null;
        var alreadyAttached = false;

        if (entity != null)
        {
            component = EnsureComp<MindContainerComponent>(entity.Value);

            if (component.HasMind)
                _ghosts.OnGhostAttempt(component.Mind.Value, false);

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
            // TODO remove this option.
            // Transfer-to-null should just detach a mind.
            // If people want to create a ghost, that should be done explicitly via some TransferToGhost() method, not
            // not implicitly via optional arguments.

            var position = Deleted(mind.OwnedEntity)
                ? _gameTicker.GetObserverSpawnPoint().ToMap(EntityManager, _transform)
                : _transform.GetMapCoordinates(mind.OwnedEntity.Value);

            entity = Spawn(GameTicker.ObserverPrototypeName, position);
            component = EnsureComp<MindContainerComponent>(entity.Value);
            var ghostComponent = Comp<GhostComponent>(entity.Value);
            _ghosts.SetCanReturnToBody(ghostComponent, false);
        }

        var oldEntity = mind.OwnedEntity;
        if (TryComp(oldEntity, out MindContainerComponent? oldContainer))
        {
            oldContainer.Mind = null;
            mind.OwnedEntity = null;
            Entity<MindComponent> mindEnt = (mindId, mind);
            Entity<MindContainerComponent> containerEnt = (oldEntity.Value, oldContainer);
            RaiseLocalEvent(oldEntity.Value, new MindRemovedMessage(mindEnt, containerEnt));
            RaiseLocalEvent(mindId, new MindGotRemovedEvent(mindEnt, containerEnt));
            Dirty(oldEntity.Value, oldContainer);
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
            RemoveVisitingEntity(mindId, mind);
        }

        // Player is CURRENTLY connected.
        var session = GetSession(mind);
        if (session != null && !alreadyAttached && mind.VisitingEntity == null)
        {
            _players.SetAttachedEntity(session, entity, true);
            DebugTools.Assert(session.AttachedEntity == entity, $"Failed to attach entity.");
            Log.Info($"Session {session.Name} transferred to entity {entity}.");
        }

        if (entity != null)
        {
            component!.Mind = mindId;
            mind.OwnedEntity = entity;
            mind.OriginalOwnedEntity ??= GetNetEntity(mind.OwnedEntity);
            Entity<MindComponent> mindEnt = (mindId, mind);
            Entity<MindContainerComponent> containerEnt = (entity.Value, component);
            RaiseLocalEvent(entity.Value, new MindAddedMessage(mindEnt, containerEnt));
            RaiseLocalEvent(mindId, new MindGotAddedEvent(mindEnt, containerEnt));
            Dirty(entity.Value, component);
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

        Dirty(mindId, mind);
        var netMind = GetNetEntity(mindId);
        _pvsOverride.ClearOverride(netMind);
        if (userId != null && !_players.TryGetPlayerData(userId.Value, out _))
        {
            Log.Error($"Attempted to set mind user to invalid value {userId}");
            return;
        }

        if (mind.Session != null)
        {
            _players.SetAttachedEntity(GetSession(mind), null);
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

        // The UserId may not have a current session, but user data may still exist for disconnected players.
        // So we cannot combine this with the TryGetSessionById() check below.
        if (_players.GetPlayerData(userId.Value).ContentData() is { } data)
            data.Mind = mindId;

        if (_players.TryGetSessionById(userId.Value, out var ret))
        {
            mind.Session = ret;
            _pvsOverride.AddSessionOverride(netMind, ret);
            _players.SetAttachedEntity(ret, mind.CurrentEntity);
        }
    }

    public override void ControlMob(EntityUid user, EntityUid target)
    {
        if (TryComp(user, out ActorComponent? actor))
            ControlMob(actor.PlayerSession.UserId, target);
    }

    public override void ControlMob(NetUserId user, EntityUid target)
    {
        var (mindId, mind) = GetOrCreateMind(user);

        if (mind.CurrentEntity == target)
            return;

        if (mind.OwnedEntity == target)
        {
            UnVisit(mindId, mind);
            return;
        }

        MakeSentientCommand.MakeSentient(target, EntityManager);
        TransferTo(mindId, target, ghostCheckOverride: true, mind: mind);
    }
}
