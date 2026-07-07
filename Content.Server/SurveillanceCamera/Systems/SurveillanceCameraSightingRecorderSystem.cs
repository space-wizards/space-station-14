using Content.Server.Power.Components;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraSightingRecorderSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceCameraSightingRecorderComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SurveillanceCameraSightingRecorderComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval * _random.NextFloat();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var cameras = EntityQueryEnumerator<SurveillanceCameraSightingRecorderComponent, SurveillanceCameraComponent>();

        while (cameras.MoveNext(out var uid, out var recorder, out var camera))
        {
            if (curTime < recorder.NextUpdate)
                continue;

            recorder.NextUpdate = curTime + recorder.UpdateInterval;

            if (!camera.Active || CompOrNull<ApcPowerReceiverComponent>(uid)?.Powered != true)
                continue;

            Log.Info($"{camera.CameraId} @ {Transform(uid).Coordinates}");
        }
    }
}
