using Content.Shared.Movement.Components;
using Content.Shared.Station.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.Replay.Spectator;

// This partial class contains functions for getting and setting the spectator's position data, so that
// a consistent view/camera can be maintained when jumping around in time.
public sealed partial class ReplaySpectatorSystem
{
    /// <summary>
    /// Simple struct containing position & rotation data for maintaining a persistent view when jumping around in time.
    /// </summary>
    public struct SpectatorData
    {
        // TODO REPLAYS handle ghost-following.

        /// <summary>
        /// The current entity being spectated.
        /// </summary>
        public EntityUid Entity;

        /// <summary>
        /// The player that was originally controlling <see cref="Entity"/>
        /// </summary>
        public NetUserId Controller;

        public (EntityCoordinates Coords, Angle Rot)? Local;
        public (EntityCoordinates Coords, Angle Rot)? World;
        public (EntityUid? Ent, Angle Rot)? Eye;
    }

    public SpectatorData GetSpectatorData()
    {
        var data = new SpectatorData();
        if (_player.LocalEntity is not { } player)
            return data;

        data.Controller = _player.LocalUser ?? DefaultUser;

        if (!TryComp(player, out TransformComponent? xform) || xform.MapUid == null)
            return data;

        data.Local = (xform.Coordinates, xform.LocalRotation);
        var (pos, rot) = _transform.GetWorldPositionRotation(player);
        data.World = (new(xform.MapUid.Value, pos), rot);

        if (TryComp(player, out InputMoverComponent? mover))
            data.Eye = (mover.RelativeEntity, mover.TargetRelativeRotation);

        data.Entity = player;

        return data;
    }

    private void OnBeforeSetTick()
    {
        _spectatorData = GetSpectatorData();
    }

    private void OnAfterSetTick()
    {
        if (_spectatorData != null)
            SetSpectatorPosition(_spectatorData.Value);
        _spectatorData = null;
    }

    private void OnBeforeApplyState((GameState Current, GameState? Next) args)
    {
        // Before applying the game state, we want to check to see if a recorded player session is about to
        // get attached to the entity that we are currently spectating. If it is, then we switch out local session
        // to the recorded session. I.e., we switch from spectating the entity to spectating the session.
        // This is required because having multiple sessions attached to a single entity is not currently supported.

        if (_player.LocalUser != DefaultUser)
            return; // Already spectating some session.

        if (_player.LocalEntity is not {} uid)
            return;

        var netEnt = GetNetEntity(uid);
        if (netEnt.IsClientSide())
            return;

        foreach (var playerState in args.Current.PlayerStates.Value)
        {
            if (playerState.ControlledEntity != netEnt)
                continue;

            if (!_player.TryGetSessionById(playerState.UserId, out var session))
                session = _player.CreateAndAddSession(playerState.UserId, playerState.Name);

            _player.SetLocalSession(session);
            break;
        }
    }

    public void SetSpectatorPosition(SpectatorData data)
    {
        if (_player.LocalSession == null)
            return;

        if (data.Controller != DefaultUser)
        {
            // the "local player" is currently set to some recorded session. As long as that session has an entity, we
            // do nothing here
            if (_player.TryGetSessionById(data.Controller, out var session)
                && Exists(session.AttachedEntity))
            {
                _player.SetLocalSession(session);
                return;
            }

            // Spectated session is no longer valid - return to the client-side session
            _player.SetLocalSession(_player.GetSessionById(DefaultUser));
        }

        if (Exists(data.Entity) && Transform(data.Entity).MapID != MapId.Nullspace)
        {
            _player.SetAttachedEntity(_player.LocalSession, data.Entity);
            return;
        }

        if (data.Local != null && data.Local.Value.Coords.IsValid(EntityManager))
        {
            var newXform = SpawnSpectatorGhost(data.Local.Value.Coords, false);
            newXform.LocalRotation = data.Local.Value.Rot;
        }
        else if (data.World != null && data.World.Value.Coords.IsValid(EntityManager))
        {
            var newXform = SpawnSpectatorGhost(data.World.Value.Coords, true);
            newXform.LocalRotation = data.World.Value.Rot;
        }
        else if (TryFindFallbackSpawn(out var coords))
        {
            var newXform = SpawnSpectatorGhost(coords, true);
            newXform.LocalRotation = 0;
        }
        else
        {
            Log.Error("Failed to find a suitable observer spawn point");
            return;
        }

        if (data.Eye != null && TryComp(_player.LocalSession.AttachedEntity, out InputMoverComponent? newMover))
        {
            newMover.RelativeEntity = data.Eye.Value.Ent;
            newMover.TargetRelativeRotation = newMover.RelativeRotation = data.Eye.Value.Rot;
        }
    }

    private bool TryFindFallbackSpawn(out EntityCoordinates coords)
    {
        if (_replayPlayback.TryGetRecorderEntity(out var recorder))
        {
            coords = new EntityCoordinates(recorder.Value, default);
            return true;
        }

        Entity<MapGridComponent>? maxUid = null;
        float? maxSize = null;
        var gridQuery = EntityQueryEnumerator<MapGridComponent>();

        var stationFound = false;
        while (gridQuery.MoveNext(out var uid, out var grid))
        {
            var size = grid.LocalAABB.Size.LengthSquared();

            var station = HasComp<StationMemberComponent>(uid);

            //We want the first station grid to overwrite any previous non-station grids no matter the size, in case the vgroid was found first
            if (maxSize is not null && size < maxSize && !(!stationFound && station))
                continue;

            if (!station && stationFound)
               continue;

            maxUid = (uid, grid);
            maxSize = size;

            if (station)
                stationFound = true;
        }

        coords = new EntityCoordinates(maxUid ?? default, default);
        return maxUid != null;
    }

    private void OnTerminating(EntityUid uid, ReplaySpectatorComponent component, ref EntityTerminatingEvent args)
    {
        if (uid != _player.LocalEntity)
            return;

        var xform = Transform(uid);
        if (xform.MapUid == null || Terminating(xform.MapUid.Value))
            return;

        SpawnSpectatorGhost(new EntityCoordinates(xform.MapUid.Value, default), true);
    }

    private void OnParentChanged(EntityUid uid, ReplaySpectatorComponent component, ref EntParentChangedMessage args)
    {
        if (uid != _player.LocalEntity)
            return;

        if (args.Transform.MapUid != null || args.OldMapId == null)
            return;

        if (_spectatorData != null)
        {
            // Currently scrubbing/setting the replay tick
            // the observer will get respawned once the state was applied
            return;
        }

        // The entity being spectated from was moved to null-space.
        // This was probably because they were spectating some entity in a client-side replay that left PVS range.
        // Simple respawn the ghost.
        SetSpectatorPosition(default);
    }

    private void OnDetached(EntityUid uid, ReplaySpectatorComponent component, LocalPlayerDetachedEvent args)
    {
        if (IsClientSide(uid))
            QueueDel(uid);
        else
            RemCompDeferred(uid, component);
    }
}
