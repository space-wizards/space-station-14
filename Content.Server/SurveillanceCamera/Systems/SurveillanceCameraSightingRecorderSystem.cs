using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraSightingRecorderSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private HashSet<Entity<MobStateComponent>> _inRange = new();

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
        var cameras = EntityQueryEnumerator<SurveillanceCameraSightingRecorderComponent, SurveillanceCameraComponent, DeviceNetworkComponent>();

        while (cameras.MoveNext(out var uid, out var recorder, out var camera, out var device))
        {
            if (curTime < recorder.NextUpdate)
                continue;

            recorder.NextUpdate = curTime + recorder.UpdateInterval;

            if (device.TransmitFrequency == null)
                continue;

            if (!camera.Active || CompOrNull<ApcPowerReceiverComponent>(uid)?.Powered != true)
                continue;

            var coords = Transform(uid).Coordinates;

            _inRange.Clear();
            _lookup.GetEntitiesInRange(coords, recorder.DetectionRange, _inRange);

            var sightings = new List<CameraSightingRecord>();

            foreach (var mob in _inRange)
            {
                if (_examine.InRangeUnOccluded(mob, coords, recorder.DetectionRange))
                {
                    var mobXform = Transform(mob);
                    if (mobXform.GridUid == null)
                        continue;

                    var gridCoords = new EntityCoordinates(mobXform.GridUid.Value,
                        Vector2.Transform(_transform.GetWorldPosition(mobXform),
                            _transform.GetInvWorldMatrix(mobXform.GridUid.Value)));

                    sightings.Add(new CameraSightingRecord(curTime, camera.CameraId,
                        GetNetCoordinates(gridCoords), Identity.Name(mob, EntityManager)));
                }
            }

            if (sightings.Count > 0)
            {
                var payload = new NetworkPayload()
                {
                    [DeviceNetworkConstants.Command] = CameraSightingConstants.NET_COMMAND_STRING,
                    [CameraSightingConstants.NET_SIGHTINGS] = sightings,
                };

                _deviceNetwork.QueuePacket(uid, null, payload, device: device);
            }
        }
    }
}
