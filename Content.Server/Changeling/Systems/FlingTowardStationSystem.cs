using Content.Server.Changeling.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Server.Station.Systems;
using Robust.Shared.Physics.Components;

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

        var random = new RobustRandom();
        var station = random.Pick(_station.GetStations());

        var stationPosition = _transform.GetMapCoordinates(station);
        var entPosition = _transform.GetMapCoordinates(ent.Owner);

        var offset = entPosition.Position - stationPosition.Position;

        var physics = Comp<PhysicsComponent>(ent.Owner);
        _physics.ApplyLinearImpulse(ent.Owner, -offset.Normalized() * ent.Comp.Speed * physics.Mass, body: physics);
    }
}
