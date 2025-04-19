using System.Numerics;
using Content.Server.Power.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Power;
using Content.Shared.Silicons;
using Content.Shared.Silicons.Bots;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Silicons;

public sealed class RobotFenceSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private const float XOff = -.125f;
    private const float YOff = -.3f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RobotFenceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RobotFenceComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<RobotFenceComponent, ComponentRemove>(OnRemove);
    }

    private void OnPowerChanged(Entity<RobotFenceComponent> ent, ref PowerChangedEvent evt)
    {
        _appearance.SetData(ent.Owner, RobotFenceVisuals.IsOn, evt.Powered);
        if (!evt.Powered)
        {
            DestroyBeam(ent);
        }
        else
        {
            UpdateFixture(ent);
        }

    }

    private void DestroyBeam(Entity<RobotFenceComponent> ent)
    {
        while (ent.Comp.BeamEntities.Count > 0)
        {
            QueueDel(ent.Comp.BeamEntities.Pop());
        }

        ent.Comp.BeamSubSteps = 0;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<RobotFenceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if(comp.NextUpdate > curTime)
                continue;

            UpdateFixture((uid, comp));

            comp.NextUpdate += comp.UpdatePeriod;
        }
    }

    private void UpdateFixture(Entity<RobotFenceComponent> ent)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;
        var entTransform = EnsureComp<TransformComponent>(ent.Owner);
        // I hate coordinate systems
        if (!entTransform.GridUid.HasValue)
            return;
        var gridRotation = _transform.GetWorldRotation(entTransform.GridUid.Value);

        var mapCoords = _transform.GetMapCoordinates(ent.Owner, entTransform);

        var worldRotation = _transform.GetWorldRotation(ent.Owner);
        var direction = Angle.FromWorldVec(worldRotation.ToVec()).Opposite();

        var localRotation = entTransform.LocalRotation;

        var startCoords = mapCoords.Position;
        startCoords -= (localRotation + gridRotation).RotateVec(new Vector2(XOff, YOff));

        // Get information ready to cast a ray
        var mapId = entTransform.MapID;
        // var mask = (int)CollisionGroup.HighImpassable;
        // var mask = (int)(CollisionGroup.Opaque | CollisionGroup.HighImpassable);
        // var mask = (int)(CollisionGroup.LowImpassable);
        var mask = (int)(CollisionGroup.InteractImpassable);

        var ray = new CollisionRay(startCoords, direction.ToVec(), mask);
        // Cast a ray to see how far the beam goes
        var res = _physics.IntersectRay(mapId, ray, (float)ent.Comp.BeamRange-YOff-0.5f, ent.Owner, false);
        // If it didn't hit anything, use the maximum range
        var distance = res.FirstOrNull()?.Distance ?? (float)ent.Comp.BeamRange;

        if (distance <= 0f)
        {
            DestroyBeam(ent);
            return;
        }

        var mapGridComp = Comp<MapGridComponent>(entTransform.GridUid.Value);

        var beamSubSteps = (int)Math.Ceiling(distance*4f/mapGridComp.TileSize);

        if (ent.Comp.BeamSubSteps == beamSubSteps)
        {
            return;
        }



        var curEndcapSubSteps = ent.Comp.BeamSubSteps % 4;
        if (curEndcapSubSteps > 0)
        {
            QueueDel(ent.Comp.BeamEntities.Pop());
        }

        var newFullSegments = beamSubSteps / 4;
        while (newFullSegments < ent.Comp.BeamEntities.Count)
        {
            QueueDel(ent.Comp.BeamEntities.Pop());
        }

        while (newFullSegments > ent.Comp.BeamEntities.Count)
        {
            var newCoords = new EntityCoordinates(ent.Owner, 0, -mapGridComp.TileSize*ent.Comp.BeamEntities.Count);
            var isEmpty = newCoords.GetTileRef(EntityManager)?.Tile.IsEmpty ?? true;
            EntityUid? newEnt = null;
            if (!isEmpty)
            {
                newEnt = SpawnAtPosition("RobotFenceSegment4", newCoords);
                _transform.SetLocalRotationNoLerp(newEnt.Value, localRotation.Opposite());
            }

            ent.Comp.BeamEntities.Add(newEnt);
        }

        var newEndcapSubSteps = beamSubSteps % 4;
        if (newEndcapSubSteps > 0)
        {
            var newCoords = new EntityCoordinates(ent.Owner, 0, -mapGridComp.TileSize*ent.Comp.BeamEntities.Count);
            var isEmpty = newCoords.GetTileRef(EntityManager)?.Tile.IsEmpty ?? true;
            EntityUid? newEnt = null;
            if (!isEmpty)
            {
                newEnt = SpawnAtPosition($"RobotFenceSegment{newEndcapSubSteps}", newCoords);
                _transform.SetLocalRotationNoLerp(newEnt.Value, localRotation.Opposite());
            }
            ent.Comp.BeamEntities.Add(newEnt);
        }

        ent.Comp.BeamSubSteps = beamSubSteps;
    }

    private void OnInit(Entity<RobotFenceComponent> ent, ref ComponentInit evt)
    {
        // We can't create the beam entity here, so just want for the first update
        _appearance.SetData(ent.Owner, RobotFenceVisuals.IsOn, _powerReceiver.IsPowered(ent.Owner));
    }

    private void OnRemove(Entity<RobotFenceComponent> ent, ref ComponentRemove evt)
    {
        // Make sure to remove the beam entity
        DestroyBeam(ent);
    }
}
