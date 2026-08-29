using Content.Shared.Station.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Physics.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared.Station.Systems;

public sealed partial class FlingTowardStationSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<FlingTowardStationComponent> ent, ref MapInitEvent args)
    {
        if (_station.GetStations().Count == 0)
            return;

        // fetch the station's grid
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner));
        var station = random.Pick(_station.GetStations());
        if (!TryComp<StationDataComponent>(station, out var stationComp))
            return;

        var grid = _station.GetLargestGrid((station, stationComp));
        if (grid == null)
            return;

        var stationPosition = _transform.GetMapCoordinates(grid.Value);
        var entPosition = _transform.GetMapCoordinates(ent.Owner);
        if (!TryComp<PhysicsComponent>(grid.Value, out var gridPhys))
            return;

        // calculate the offset from the center of mass of the station to the body
        var offset = entPosition.Position - _transform.GetWorldRotation(grid.Value).RotateVec(gridPhys.LocalCenter) - stationPosition.Position;

        if (!TryComp<PhysicsComponent>(ent.Owner, out var physics))
            return;

        _physics.ApplyLinearImpulse(ent.Owner, -offset.Normalized() * ent.Comp.Speed * physics.Mass, body: physics);
    }
}
