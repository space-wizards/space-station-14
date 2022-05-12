using System.Linq;
using Content.Server.Station.Systems;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraStationNetworkSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
    }

    public void OnStationInitialize(StationInitializedEvent args)
    {
        EnsureComp<SurveillanceCameraStationNetworkComponent>(args.Station);

        RaiseLocalEvent(new PopulateSurveillanceCameraNetwork(args.Station));
    }

    public void AddCamera(EntityUid station, EntityUid camera, SurveillanceCameraComponent? cameraComponent = null,
        SurveillanceCameraStationNetworkComponent? network = null)
    {
        if (!Resolve(station, ref network)
            || !Resolve(camera, ref cameraComponent))
        {
            return;
        }

        if (!network.CameraSubnets.ContainsKey(cameraComponent.Subnet))
        {
            var cameraSubnet = new HashSet<EntityUid>();
            cameraSubnet.Add(camera);

            network.CameraSubnets.Add(cameraComponent.Subnet, cameraSubnet);
        }
        else
        {
            network.CameraSubnets[cameraComponent.Subnet].Add(camera);
        }
    }
}

public sealed class PopulateSurveillanceCameraNetwork : EntityEventArgs
{
    public EntityUid Station { get; }

    public PopulateSurveillanceCameraNetwork(EntityUid station)
    {
        Station = station;
    }
}
