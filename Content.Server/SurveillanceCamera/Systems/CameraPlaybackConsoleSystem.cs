using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.SurveillanceCamera;
using Robust.Server.GameObjects;

namespace Content.Server.SurveillanceCamera;

public sealed partial class CameraPlaybackConsoleSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<CameraPlaybackConsoleComponent>(CameraPlaybackConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<CameraPlaybackTargetRequestMessage>(OnTargetRequest);
        });
    }

    private void OnUiOpened(EntityUid uid, CameraPlaybackConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUi(uid, null);
    }

    private void OnTargetRequest(EntityUid uid, CameraPlaybackConsoleComponent component, CameraPlaybackTargetRequestMessage args)
    {
        UpdateUi(uid, args.Target);
    }

    private void UpdateUi(EntityUid uid, TimeSpan? target)
    {
        SurveillanceRecordsServerComponent? records = null;
        var consoleGrid = Transform(uid).GridUid;

        var servers = EntityQueryEnumerator<SurveillanceRecordsServerComponent, TransformComponent>();
        while (servers.MoveNext(out var serverUid, out var server, out var xform))
        {
            if (xform.GridUid != consoleGrid)
                continue;

            if (CompOrNull<ApcPowerReceiverComponent>(serverUid)?.Powered != true)
                continue;

            records = server;
            break;
        }

        if (records == null || records.Records.Count == 0)
        {
            _ui.SetUiState(uid, CameraPlaybackConsoleKey.Key,
                new CameraPlaybackConsoleState(new List<CameraSightingRecord>(), TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero));
            return;
        }

        var oldest = records.Records.Peek().Time;
        var newest = records.Records.Last().Time;

        var clamped = target ?? newest;
        if (clamped < oldest)
            clamped = oldest;
        if (clamped > newest)
            clamped = newest;

        var half = CameraPlaybackConstants.SliceWindow / 2;
        var slice = new List<CameraSightingRecord>();
        foreach (var record in records.Records)
        {
            if ((record.Time - clamped).Duration() <= half)
                slice.Add(record);
        }

        _ui.SetUiState(uid, CameraPlaybackConsoleKey.Key,
            new CameraPlaybackConsoleState(slice, oldest, newest, clamped));
    }
}
