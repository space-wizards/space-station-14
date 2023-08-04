using System.Linq;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Replay.Spectator;

// This partial class contains functions for getting and setting the spectator's position data, so that
// a consistent view/camera can be maintained when jumping around in time.
public sealed partial class ReplaySpectatorSystem
{
    /// <summary>
    /// Simple struct containing position & rotation data for maintaining a persistent view when jumping around in time.
    /// </summary>
    public struct SpectatorPosition
    {
        // TODO REPLAYS handle ghost-following.
        public EntityUid Entity;
        public (EntityCoordinates Coords, Angle Rot)? Local;
        public (EntityCoordinates Coords, Angle Rot)? World;
        public (EntityUid? Ent, Angle Rot)? Eye;
    }

    public SpectatorPosition GetSpectatorPosition()
    {
        var obs = new SpectatorPosition();
        if (_player.LocalPlayer?.ControlledEntity is { } player && TryComp(player, out TransformComponent? xform) && xform.MapUid != null)
        {
            obs.Local = (xform.Coordinates, xform.LocalRotation);
            obs.World = (new(xform.MapUid.Value, xform.WorldPosition), xform.WorldRotation);

            if (TryComp(player, out InputMoverComponent? mover))
                obs.Eye = (mover.RelativeEntity, mover.TargetRelativeRotation);

            obs.Entity = player;
        }

        return obs;
    }

    private void OnBeforeSetTick()
    {
        _oldPosition = GetSpectatorPosition();
    }

    private void OnAfterSetTick()
    {
        if (_oldPosition != null)
            SetSpectatorPosition(_oldPosition.Value);
        _oldPosition = null;
    }

    public void SetSpectatorPosition(SpectatorPosition spectatorPosition)
    {
        if (Exists(spectatorPosition.Entity) && Transform(spectatorPosition.Entity).MapID != MapId.Nullspace)
        {
            _player.LocalPlayer!.AttachEntity(spectatorPosition.Entity, EntityManager, _client);
            return;
        }

        if (spectatorPosition.Local != null && spectatorPosition.Local.Value.Coords.IsValid(EntityManager))
        {
            var newXform = SpawnSpectatorGhost(spectatorPosition.Local.Value.Coords, false);
            newXform.LocalRotation = spectatorPosition.Local.Value.Rot;
        }
        else if (spectatorPosition.World != null && spectatorPosition.World.Value.Coords.IsValid(EntityManager))
        {
            var newXform = SpawnSpectatorGhost(spectatorPosition.World.Value.Coords, true);
            newXform.LocalRotation = spectatorPosition.World.Value.Rot;
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

        if (spectatorPosition.Eye != null && TryComp(_player.LocalPlayer?.ControlledEntity, out InputMoverComponent? newMover))
        {
            newMover.RelativeEntity = spectatorPosition.Eye.Value.Ent;
            newMover.TargetRelativeRotation = newMover.RelativeRotation = spectatorPosition.Eye.Value.Rot;
        }
    }

    private bool TryFindFallbackSpawn(out EntityCoordinates coords)
    {
        var uid = EntityQuery<MapGridComponent>()
            .OrderByDescending(x => x.LocalAABB.Size.LengthSquared)
            .FirstOrDefault()?.Owner;
        coords = new EntityCoordinates(uid ?? default, default);
        return uid != null;
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

    private void OnDetached(EntityUid uid, ReplaySpectatorComponent component, PlayerDetachedEvent args)
    {
        if (uid.IsClientSide())
            QueueDel(uid);
        else
            RemCompDeferred(uid, component);
    }
}
