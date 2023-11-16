using Content.Server.DeviceLinking.Components;
﻿using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceLinking;

namespace Content.Server.DeviceLinking.Systems;

public sealed class DeviceLinkSystem : SharedDeviceLinkSystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceLinkSinkComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DeviceLinkSinkComponent>();

        while (query.MoveNext(out var component))
        {
            if (component.InvokeLimit < 1)
            {
                component.InvokeCounter = 0;
                continue;
            }

            if(component.InvokeCounter > 0)
                component.InvokeCounter--;
        }
    }

    #region Sending & Receiving
    /// <summary>
    /// Sends a network payload directed at the sink entity.
    /// Just raises a <see cref="SignalReceivedEvent"/> without data if the source or the sink doesn't have a <see cref="DeviceNetworkComponent"/>
    /// </summary>
    /// <param name="uid">The source uid that invokes the port</param>
    /// <param name="port">The port to invoke</param>
    /// <param name="data">Optional data to send along</param>
    /// <param name="sourceComponent"></param>
    public void InvokePort(EntityUid uid, string port, NetworkPayload? data = null, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(uid, ref sourceComponent) || !sourceComponent.Outputs.TryGetValue(port, out var sinks))
            return;

        foreach (var sinkUid in sinks)
        {
            if (!sourceComponent.LinkedPorts.TryGetValue(sinkUid, out var links))
                continue;

            if (!TryComp<DeviceLinkSinkComponent>(sinkUid, out var sinkComponent))
                continue;

            foreach (var (source, sink) in links)
            {
                if (source != port)
                    continue;

                if (sinkComponent.InvokeCounter > sinkComponent.InvokeLimit)
                {
                    sinkComponent.InvokeCounter = 0;
                    var args = new DeviceLinkOverloadedEvent();
                    RaiseLocalEvent(sinkUid, ref args);
                    RemoveAllFromSink(sinkUid, sinkComponent);
                    continue;
                }

                sinkComponent.InvokeCounter++;

                //Just skip using device networking if the source or the sink doesn't support it
                if (!HasComp<DeviceNetworkComponent>(uid) || !TryComp<DeviceNetworkComponent>(sinkUid, out var sinkNetworkComponent))
                {
                    var eventArgs = new SignalReceivedEvent(sink, uid);

                    RaiseLocalEvent(sinkUid, ref eventArgs);
                    continue;
                }

                var payload = new NetworkPayload()
                {
                    [InvokedPort] = sink
                };

                if (data != null)
                {
                    //Prevent overriding the invoked port
                    data.Remove(InvokedPort);
                    foreach (var (key, value) in data)
                    {
                        payload.Add(key, value);
                    }
                }

                // force using wireless network so things like atmos devices are able to send signals
                var network = (int) DeviceNetworkComponent.DeviceNetIdDefaults.Wireless;
                _deviceNetworkSystem.QueuePacket(uid, sinkNetworkComponent.Address, payload, sinkNetworkComponent.ReceiveFrequency, network);
            }
        }
    }

    /// <summary>
    /// Helper function that invokes a port with a high/low binary logic signal.
    /// </summary>
    public void SendSignal(EntityUid uid, string port, bool signal, DeviceLinkSourceComponent? comp = null)
    {
        var data = new NetworkPayload
        {
            [DeviceNetworkConstants.LogicState] = signal ? SignalState.High : SignalState.Low
        };
        InvokePort(uid, port, data, comp);
    }

    /// <summary>
    /// Checks if the payload has a port defined and if the port is present on the sink.
    /// Raises a <see cref="SignalReceivedEvent"/> containing the payload when the check passes
    /// </summary>
    private void OnPacketReceived(EntityUid uid, DeviceLinkSinkComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(InvokedPort, out string? port) || !(component.Ports?.Contains(port) ?? false))
            return;

        var eventArgs = new SignalReceivedEvent(port, args.Sender, args.Data);
        RaiseLocalEvent(uid,  ref eventArgs);
    }
    #endregion
}
