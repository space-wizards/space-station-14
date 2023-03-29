using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.MachineLinking.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Utility;

namespace Content.Server.DeviceLinking.Systems;

public sealed class DeviceLinkSystem : SharedDeviceLinkSystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SignalTransmitterComponent, MapInitEvent>(OnTransmitterStartup);
        SubscribeLocalEvent<DeviceLinkSinkComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    /// <summary>
    /// Moves existing links from machine linking to device linking to ensure linked things still work even when the map wasn't updated yet
    /// </summary>
    private void OnTransmitterStartup(EntityUid sourceUid, SignalTransmitterComponent transmitterComponent, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent?>(sourceUid, out var sourceComponent))
            return;

        Dictionary<EntityUid, List<(string, string)>> outputs = new();
        foreach (var (transmitterPort, receiverPorts) in transmitterComponent.Outputs)
        {

            foreach (var receiverPort in receiverPorts)
            {
                outputs.GetOrNew(receiverPort.Uid).Add((transmitterPort, receiverPort.Port));
            }
        }

        foreach (var (sinkUid, links) in outputs)
        {
            SaveLinks(null, sourceUid, sinkUid, links, sourceComponent);
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

            foreach (var (source, sink) in links)
            {
                if (source != port)
                    continue;

                //Just skip using device networking if the source or the sink doesn't support it
                if (!HasComp<DeviceNetworkComponent>(uid) || !TryComp<DeviceNetworkComponent?>(sinkUid, out var sinkNetworkComponent))
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

                _deviceNetworkSystem.QueuePacket(uid, sinkNetworkComponent.Address, payload, sinkNetworkComponent.ReceiveFrequency);
            }
        }
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
