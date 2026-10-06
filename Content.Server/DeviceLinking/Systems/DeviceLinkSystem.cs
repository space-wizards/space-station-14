using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class DeviceLinkSystem : SharedDeviceLinkSystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: make an engine PR to allow for auto-generated relay subscriptions
        // Should be doable by using reflection on marker interfaces and then adding them to the auto-generated subscriptions
        // I know it looks absolutely hilarious and horrible, but uuuhhhh anything to avoid boxing allocations!!!!!!!! :godo:
        SubscribeLocalEvent<DeviceLinkSinkComponent, DeviceNetworkPacketEvent<SignalPayload<LogicStatePayload>>>(OnSignalReceived);
    }

    private void OnSignalReceived<T>(Entity<DeviceLinkSinkComponent> ent,
        ref DeviceNetworkPacketEvent<SignalPayload<T>> args) where T : ISignalNetworkPayload
    {
        var (uid, component) = ent;
        if (!component.Ports.Contains(args.Data.InvokedPort))
            return;

        var eventArgs = new SignalReceivedEvent<T>(args.Data.InvokedPort, args.Data.Payload, args.Sender);
        RaiseLocalEvent(uid, ref eventArgs);
    }

    #region Sending & Receiving

    public override void InvokePort(Entity<DeviceLinkSourceComponent?> ent, string port)
    {
        if (!DeviceLinkSourceQuery.Resolve(ent.Owner, ref ent.Comp) ||
            !ent.Comp.Outputs.TryGetValue(port, out var sinks))
            return;

        foreach (var sinkUid in sinks)
        {
            if (!ent.Comp.LinkedPorts.TryGetValue(sinkUid, out var links))
                continue;

            if (!DeviceLinkSinkQuery.TryComp(sinkUid, out var sinkComponent))
                continue;

            foreach (var (source, sink) in links)
            {
                if (source == port)
                    InvokeDirect((ent.Owner, ent.Comp), (sinkUid, sinkComponent), sink);
            }
        }
    }

    public override void InvokePort<T>(Entity<DeviceLinkSourceComponent?> ent, string port, ref T data)
    {
        if (!DeviceLinkSourceQuery.Resolve(ent.Owner, ref ent.Comp) ||
            !ent.Comp.Outputs.TryGetValue(port, out var sinks))
            return;

        foreach (var sinkUid in sinks)
        {
            if (!ent.Comp.LinkedPorts.TryGetValue(sinkUid, out var links))
                continue;

            if (!DeviceLinkSinkQuery.TryComp(sinkUid, out var sinkComponent))
                continue;

            foreach (var (source, sink) in links)
            {
                if (source == port)
                    InvokeDirect(ent!, (sinkUid, sinkComponent), sink, ref data);
            }
        }
    }

    /// <summary>
    /// Raises an event on or sends a network packet directly to a sink from a source.
    /// </summary>
    private void InvokeDirect(Entity<DeviceLinkSourceComponent> source,
        Entity<DeviceLinkSinkComponent?> sink,
        string sinkPort)
    {
        if (!DeviceLinkSinkQuery.Resolve(sink, ref sink.Comp))
            return;

        var invokeCounter = GetEffectiveInvokeCounter(sink.Comp);
        if (invokeCounter > sink.Comp.InvokeLimit)
        {
            SetInvokeCounter(sink.Comp, 0);
            var args = new DeviceLinkOverloadedEvent();
            RaiseLocalEvent(sink, ref args);
            RemoveAllFromSink(sink, sink.Comp);
            return;
        }

        SetInvokeCounter(sink.Comp, invokeCounter + 1);

        //Just skip using device networking if the source or the sink doesn't support it
        if (!HasComp<DeviceNetworkComponent>(source) || !TryComp<DeviceNetworkComponent>(sink, out var sinkNetwork))
        {
            var eventArgs = new SignalReceivedEvent(sinkPort, source);
            RaiseLocalEvent(sink, ref eventArgs);
            return;
        }

        var payload = new SignalPayload
        {
            InvokedPort = sinkPort,
        };

        // force using wireless network so things like atmos devices are able to send signals
        _deviceNetworkSystem.SendPacket(source.Owner,
            sinkNetwork.Address,
            ref payload,
            sinkNetwork.ReceiveFrequency,
            (int)DeviceNetIdDefaults.Wireless);
    }

    /// <summary>
    /// Raises an event on or sends a network packet directly to a sink from a source.
    /// </summary>
    private void InvokeDirect<T>(Entity<DeviceLinkSourceComponent> source,
        Entity<DeviceLinkSinkComponent?> sink,
        string sinkPort,
        ref T data) where T : ISignalNetworkPayload
    {
        if (!DeviceLinkSinkQuery.Resolve(sink, ref sink.Comp))
            return;

        var invokeCounter = GetEffectiveInvokeCounter(sink.Comp);
        if (invokeCounter > sink.Comp.InvokeLimit)
        {
            SetInvokeCounter(sink.Comp, 0);
            var args = new DeviceLinkOverloadedEvent();
            RaiseLocalEvent(sink, ref args);
            RemoveAllFromSink(sink, sink.Comp);
            return;
        }

        SetInvokeCounter(sink.Comp, invokeCounter + 1);

        //Just skip using device networking if the source or the sink doesn't support it
        if (!HasComp<DeviceNetworkComponent>(source) || !TryComp<DeviceNetworkComponent>(sink, out var sinkNetwork))
        {
            var eventArgs = new SignalReceivedEvent(sinkPort, source);
            RaiseLocalEvent(sink, ref eventArgs);
            return;
        }

        var payload = new SignalPayload<T>
        {
            InvokedPort = sinkPort,
            Payload = data,
        };

        // force using wireless network so things like atmos devices are able to send signals
        _deviceNetworkSystem.SendPacket(source.Owner,
            sinkNetwork.Address,
            ref payload,
            sinkNetwork.ReceiveFrequency,
            (int)DeviceNetIdDefaults.Wireless);
    }

    /// <summary>
    /// Helper function that invokes a port with a high/low binary logic signal.
    /// </summary>
    public void SendSignal(Entity<DeviceLinkSourceComponent?> ent, string port, bool signal)
    {
        if (!DeviceLinkSourceQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        var data = new LogicStatePayload
        {
            State = signal ? SignalState.High : SignalState.Low
        };
        InvokePort(ent, port, ref data);

        ent.Comp.LastSignals[port] = signal;
    }

    /// <summary>
    /// Clears the last signals state for linking.
    /// This is not to be confused with sending a low signal, this is the complete absence of anything.
    /// Use if the device is in an invalid state and has no reasonable output signal.
    /// </summary>
    public void ClearSignal(Entity<DeviceLinkSourceComponent?> ent, string port)
    {
        if (!DeviceLinkSourceQuery.Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.LastSignals.Remove(port);
    }

    /// <summary>
    /// Checks if the payload has a port defined and if the port is present on the sink.
    /// Raises a <see cref="SignalReceivedEvent"/> containing the payload when the check passes
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<DeviceLinkSinkComponent> ent, ref DeviceNetworkPacketEvent<SignalPayload> args)
    {
        var (uid, component) = ent;
        if (!component.Ports.Contains(args.Data.InvokedPort))
            return;

        var eventArgs = new SignalReceivedEvent(args.Data.InvokedPort, args.Sender);
        RaiseLocalEvent(uid, ref eventArgs);
    }

    /// <summary>
    /// When linking from a port that currently has a signal being sent, invoke the new link with that signal.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnNewLink(Entity<DeviceLinkSourceComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner)
            return;

        // only do anything if a signal is being sent from a port
        if (!ent.Comp.LastSignals.TryGetValue(args.SourcePort, out var signal))
            return;

        var payload = new LogicStatePayload
        {
            State = signal ? SignalState.High : SignalState.Low
        };
        InvokeDirect(ent, args.Sink, args.SinkPort, ref payload);
    }

    #endregion
}
