using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.SurveillanceCamera;
using Robust.Shared.Timing;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceRecordsServerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceRecordsServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(EntityUid uid,
        SurveillanceRecordsServerComponent component,
        DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != CameraSightingConstants.NET_COMMAND_STRING)
            return;

        if (!args.Data.TryGetValue(CameraSightingConstants.NET_SIGHTINGS, out List<CameraSightingRecord>? sightings))
            return;

        foreach (var record in sightings)
        {
            record.CameraAddress = args.SenderAddress;
            component.Records.Enqueue(record);
        }

        while (component.Records.Count > component.MaxRecords)
        {
            component.Records.Dequeue();
        }

        var curTime = _timing.CurTime;
        while (component.Records.Count > 0 && curTime - component.Records.Peek().Time > component.Retention)
        {
            component.Records.Dequeue();
        }
    }
}
