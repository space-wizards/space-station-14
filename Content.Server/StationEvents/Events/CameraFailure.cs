using System.Linq;
using Content.Server.SurveillanceCamera;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class CameraFailure : StationEventSystem
{
    [Dependency] private readonly SurveillanceCameraSystem _cameraSystem = default!;

    public override string Prototype => "CameraFailure";

    public override void Started()
    {
        base.Started();

        var modifier = GetSeverityModifier();

        var cameras = EntityQuery<SurveillanceCameraComponent>().ToList();
        RobustRandom.Shuffle(cameras);

        var cameraAmount = (int) (RobustRandom.Next(1, 3) * Math.Sqrt(modifier));

        for (var i = 0; i < cameraAmount && i < cameras.Count - 1; i++)
        {
            var camera = RobustRandom.Pick(cameras);
            Sawmill.Info($"Disabling {camera}");

            _cameraSystem.SetActive(camera.Owner, false);
        }
    }
}
