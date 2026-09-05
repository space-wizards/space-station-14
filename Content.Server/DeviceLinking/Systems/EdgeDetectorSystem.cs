using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class EdgeDetectorSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EdgeDetectorComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, EdgeDetectorComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputHighPort, comp.OutputLowPort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(EntityUid uid, EdgeDetectorComponent comp, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        var state = args.Data.State;

        if (args.Port != comp.InputPort)
            return;

        // make sure the level changed, multiple devices sending the same level are treated as one spamming
        if (comp.State == state)
            return;

        comp.State = state;

        var port = state == SignalState.High ? comp.OutputHighPort : comp.OutputLowPort;
        _deviceLink.InvokePort(uid, port);
    }
}
