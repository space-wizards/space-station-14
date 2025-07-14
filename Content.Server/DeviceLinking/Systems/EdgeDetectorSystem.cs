using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server.DeviceLinking.Systems;

public sealed class EdgeDetectorSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EdgeDetectorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EdgeDetectorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, EdgeDetectorComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputHighPort, comp.OutputLowPort);
    }

    private void OnSignalReceived(EntityUid uid, EdgeDetectorComponent comp, ref SignalReceivedEvent args)
    {
        // only handle signals with edges
        var state = SignalState.Momentary;
        if (args.Data == null ||
            !args.Data.TryGetValue(DeviceNetworkConstants.LogicState, out state) ||
            state == SignalState.Momentary)
            return;

        if (args.Port != comp.InputPort)
            return;

        // make sure the level changed, multiple devices sending the same level are treated as one spamming
        if (comp.State != state)
        {
            comp.State = state;

            var port = state == SignalState.High ? comp.OutputHighPort : comp.OutputLowPort;
            _deviceLink.InvokePort(uid, port);
        }
    }
}
