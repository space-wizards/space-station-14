using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class EdgeDetectorSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<EdgeDetectorComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.InputPort);
        _deviceLink.EnsureSourcePorts(ent.Owner, ent.Comp.OutputHighPort, ent.Comp.OutputLowPort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<EdgeDetectorComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        var state = args.Data.State;

        if (args.Port != ent.Comp.InputPort)
            return;

        // make sure the level changed, multiple devices sending the same level are treated as one spamming
        if (ent.Comp.State == state)
            return;

        ent.Comp.State = state;

        var port = state == SignalState.High ? ent.Comp.OutputHighPort : ent.Comp.OutputLowPort;
        _deviceLink.InvokePort(ent.Owner, port);
    }
}
