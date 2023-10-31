using Content.Shared.Movement.Components;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;

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
        public NetUserId? Controller;

        public (EntityCoordinates Coords, Angle Rot)? Local;
        public (EntityCoordinates Coords, Angle Rot)? World;
        public (EntityUid? Ent, Angle Rot)? Eye;
    }

    public SpectatorData GetSpectatorData()
    {
        var data = new SpectatorData();

        if (_player.LocalPlayer?.ControlledEntity is not { } player)
            return data;

        foreach (var session in _player.Sessions)
        {
            if (session.UserId == _player.LocalPlayer?.UserId)
                continue;

            if (session.AttachedEntity == player)
            {
                data.Controller = session.UserId;
                break;
            }
        }

        if (!TryComp(player, out TransformComponent? xform) || xform.MapUid == null)
            return data;

        data.Local = (xform.Coordinates, xform.LocalRotation);
        data.World = (new(xform.MapUid.Value, xform.WorldPosition), xform.WorldRotation);

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

    public void SetSpectatorPosition(SpectatorData data)
    {
        if (_player.LocalSession == null)
            return;

        if (data.Controller != null
            && _player.SessionsDict.TryGetValue(data.Controller.Value, out var session)
            && Exists(session.AttachedEntity)
            && Transform(session.AttachedEntity.Value).MapID != MapId.Nullspace)
        {
            _player.SetAttachedEntity(_player.LocalSession, session.AttachedEntity);
            return;
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
            Logger.Error("Failed to find a suitable observer spawn point");
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

        while (gridQuery.MoveNext(out var uid, out var grid))
        {
            var size = grid.LocalAABB.Size.LengthSquared();
            if (maxSize == null || size > maxSize)
            {
                maxUid = (uid, grid);
                maxSize = size;
            }
        }

        coords = new EntityCoordinates(maxUid ?? default, default);
        return maxUid != null;
    }

    private void OnTerminating(EntityUid uid, ReplaySpectatorComponent component, ref EntityTerminatingEvent args)
    {
        if (uid != _player.LocalPlayer?.ControlledEntity)
            return;

        var xform = Transform(uid);
        if (xform.MapUid == null || Terminating(xform.MapUid.Value))
            return;

        SpawnSpectatorGhost(new EntityCoordinates(xform.MapUid.Value, default), true);
    }

    private void OnParentChanged(EntityUid uid, ReplaySpectatorComponent component, ref EntParentChangedMessage args)
    {
        if (uid != _player.LocalPlayer?.ControlledEntity)
            return;

        if (args.Transform.MapUid != null || args.OldMapId == MapId.Nullspace)
            return;

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
