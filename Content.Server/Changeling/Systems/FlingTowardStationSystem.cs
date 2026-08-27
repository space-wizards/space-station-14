using Content.Server.Changeling.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Server.Station.Systems;
using Robust.Shared.Physics.Components;
using Content.Shared.Station.Components;

namespace Content.Server.Changeling.Systems;

public sealed partial class FlingTowardStationSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<FlingTowardStationComponent> ent, ref MapInitEvent args)
    {
        if (_station.GetStations().Count == 0)
            return;

        // fetch the station's grid
        var random = new RobustRandom();
        var station = random.Pick(_station.GetStations());
        if (!TryComp<StationDataComponent>(station, out var stationComp))
            return;

        var grid = _station.GetLargestGrid((station, stationComp));
        if (grid == null)
            return;

        var stationPosition = _transform.GetMapCoordinates(grid.Value);
        var entPosition = _transform.GetMapCoordinates(ent.Owner);
        var gridPhys = Comp<PhysicsComponent>(grid.Value);

        // calculate the offset from the center of mass of the station to the body
        var offset = entPosition.Position - _transform.GetWorldRotation(grid.Value).RotateVec(gridPhys.LocalCenter) - stationPosition.Position;

        var physics = Comp<PhysicsComponent>(ent.Owner);
        _physics.ApplyLinearImpulse(ent.Owner, -offset.Normalized() * ent.Comp.Speed * physics.Mass, body: physics);
    }
}
