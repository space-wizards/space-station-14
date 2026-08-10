using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class DeviceLinkSystem
{
    /// <summary>
    /// Sends a network payload directed at the sink entity.
    /// Just raises a <see cref="SignalReceivedEvent"/> without data if the source or the sink doesn't have a <see cref="DeviceNetworkComponent"/>
    /// </summary>
    /// <param name="ent">The source that invokes the port</param>
    /// <param name="port">The port to invoke</param>
    public void InvokePort(Entity<DeviceLinkSourceComponent?> ent, string port)
    {
        if (!_deviceLinkSourceQuery.Resolve(ent.Owner, ref ent.Comp)
            || !ent.Comp.Outputs.TryGetValue(port, out var sinks))
            return;

        foreach (var sinkUid in sinks)
        {
            if (!ent.Comp.LinkedPorts.TryGetValue(sinkUid, out var links))
                continue;

            if (!_deviceLinkSinkQuery.TryComp(sinkUid, out var sinkComponent))
                continue;

            foreach (var (source, sink) in links)
            {
                if (source == port)
                    InvokeDirect(ent!, (sinkUid, sinkComponent), sink);
            }
        }
    }

    /// <summary>
    /// Sends a network payload directed at the sink entity.
    /// Just raises a <see cref="SignalReceivedEvent"/> without data if the source or the sink doesn't have a <see cref="DeviceNetworkComponent"/>
    /// </summary>
    /// <param name="ent">The source that invokes the port</param>
    /// <param name="port">The port to invoke</param>
    /// <param name="data">Optional data to send along</param>
    public void InvokePort<T>(Entity<DeviceLinkSourceComponent?> ent, string port, ref T data) where T : ISignalNetworkPayload
    {
        if (!_deviceLinkSourceQuery.Resolve(ent.Owner, ref ent.Comp)
            || !ent.Comp.Outputs.TryGetValue(port, out var sinks))
            return;

        foreach (var sinkUid in sinks)
        {
            if (!ent.Comp.LinkedPorts.TryGetValue(sinkUid, out var links))
                continue;

            if (!_deviceLinkSinkQuery.TryComp(sinkUid, out var sinkComponent))
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
    private void InvokeDirect<T>(
        Entity<DeviceLinkSourceComponent> source,
        Entity<DeviceLinkSinkComponent?> sink,
        ProtoId<SinkPortPrototype> sinkPort,
        ref T data)
        where T : ISignalNetworkPayload
    {
        if (!_deviceLinkSinkQuery.Resolve(sink, ref sink.Comp))
            return;

        var invokeCounter = GetEffectiveInvokeCounter(sink.Comp);
        if (invokeCounter > sink.Comp.InvokeLimit)
        {
            SetInvokeCounter(sink!, 0);
            var args = new DeviceLinkOverloadedEvent();
            RaiseLocalEvent(sink, ref args);
            RemoveAllFromSink(sink);
            return;
        }

        SetInvokeCounter(sink!, invokeCounter + 1);

        //Just skip using device networking if the source or the sink doesn't support it
        if (!_deviceNetworkQuery.HasComp(source) || !_deviceNetworkQuery.TryComp(sink, out var sinkNetwork))
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
        var network = (int) DeviceNetIdDefaults.Wireless;
        _deviceNetworkSystem.SendPacket(source.Owner, sinkNetwork.Data.AddressId, ref payload, sinkNetwork.Data.ReceiveFrequency, network);
    }

    /// <summary>
    /// Raises an event on or sends a network packet directly to a sink from a source.
    /// </summary>
    private void InvokeDirect(
        Entity<DeviceLinkSourceComponent> source,
        Entity<DeviceLinkSinkComponent?> sink,
        ProtoId<SinkPortPrototype> sinkPort)
    {
        if (!_deviceLinkSinkQuery.Resolve(sink, ref sink.Comp))
            return;

        var invokeCounter = GetEffectiveInvokeCounter(sink.Comp);
        if (invokeCounter > sink.Comp.InvokeLimit)
        {
            SetInvokeCounter(sink!, 0);
            var args = new DeviceLinkOverloadedEvent();
            RaiseLocalEvent(sink, ref args);
            RemoveAllFromSink(sink);
            return;
        }

        SetInvokeCounter(sink!, invokeCounter + 1);

        //Just skip using device networking if the source or the sink doesn't support it
        if (!_deviceNetworkQuery.HasComp(source) || !_deviceNetworkQuery.TryComp(sink, out var sinkNetwork))
        {
            var eventArgs = new SignalReceivedEvent(sinkPort, source);
            RaiseLocalEvent(sink, ref eventArgs);
            return;
        }

        var payload = new SignalPayload
        {
            InvokedPort = sinkPort,
        };

        // Force using wireless network so things like atmos devices are able to send signals.
        _deviceNetworkSystem.SendPacket(
            source.Owner,
            sinkNetwork.Data.AddressId,
            ref payload,
            sinkNetwork.Data.ReceiveFrequency,
            (int) DeviceNetIdDefaults.Wireless);
    }

    /// <summary>
    /// Helper function that invokes a port with a high/low binary logic signal.
    /// </summary>
    public void SendSignal(Entity<DeviceLinkSourceComponent?> ent, string port, bool signal)
    {
        if (!_deviceLinkSourceQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        var data = new LogicStatePayload
        {
            State = signal ? SignalState.High : SignalState.Low
        };
        InvokePort(ent, port, ref data);

        ent.Comp.LastSignals[port] = signal;
        Dirty(ent);
    }

    /// <summary>
    /// Clears the last signals state for linking.
    /// This is not to be confused with sending a low signal, this is the complete absence of anything.
    /// Use if the device is in an invalid state and has no reasonable output signal.
    /// </summary>
    public void ClearSignal(Entity<DeviceLinkSourceComponent?> ent, string port)
    {
        if (!_deviceLinkSourceQuery.Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.LastSignals.Remove(port);
        Dirty(ent);
    }

    /// <summary>
    /// Checks if the payload has a port defined and if the port is present on the sink.
    /// Raises a <see cref="SignalReceivedEvent"/> containing the payload when the check passes
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<DeviceLinkSinkComponent> ent, ref DeviceNetworkPacketEvent<SignalPayload> args)
    {
        var (uid, component) = ent;
        if (!component.Ports.Contains(args.Data.InvokedPort))
            return;

        var eventArgs = new SignalReceivedEvent(args.Data.InvokedPort, args.Sender);
        RaiseLocalEvent(uid,  ref eventArgs);
    }

    // TODO replace this with an auto-generated relay once RT supports generic event subscriptions
    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<DeviceLinkSinkComponent> ent, ref DeviceNetworkPacketEvent<SignalPayload<LogicStatePayload>> args)
    {
        var (uid, component) = ent;
        if (!component.Ports.Contains(args.Data.InvokedPort))
            return;

        var eventArgs = new SignalReceivedEvent<LogicStatePayload>(args.Data.InvokedPort, args.Data.Payload, args.Sender);
        RaiseLocalEvent(uid,  ref eventArgs);
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

        var payload = new LogicStatePayload { State = signal ? SignalState.High : SignalState.Low };
        InvokeDirect(ent, args.Sink, args.SinkPort, ref payload);
    }
}
