using System.Linq;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.MachineLinking.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.DeviceLinking.Systems;

public sealed class DeviceLinkSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    public const string InvokedPort = "link_port";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SignalTransmitterComponent, MapInitEvent>(OnTransmitterStartup);

        SubscribeLocalEvent<DeviceLinkSourceComponent, ComponentStartup>(OnSourceStartup);
        SubscribeLocalEvent<DeviceLinkSinkComponent, ComponentStartup>(OnSinkStartup);
        SubscribeLocalEvent<DeviceLinkSourceComponent, ComponentRemove>(OnSourceRemoved);
        SubscribeLocalEvent<DeviceLinkSinkComponent, ComponentRemove>(OnSinkRemoved);
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

    #region Link Validation
    private void OnSourceStartup(EntityUid sourceUid, DeviceLinkSourceComponent sourceComponent, ComponentStartup args)
    {
        List<EntityUid> invalidSinks = new();
        foreach (var sinkUid  in sourceComponent.LinkedPorts.Keys)
        {
            if (!TryComp<DeviceLinkSinkComponent?>(sinkUid, out var sinkComponent))
            {
                invalidSinks.Add(sinkUid);
                foreach (var savedSinks in sourceComponent.Outputs.Values)
                {
                    savedSinks.Remove(sinkUid);
                }

                continue;
            }

            sinkComponent.LinkedSources.Add(sourceUid);
        }

        foreach (var invalidSink in invalidSinks)
        {
            sourceComponent.LinkedPorts.Remove(invalidSink);
        }
    }

    private void OnSinkStartup(EntityUid sinkUid, DeviceLinkSinkComponent sinkComponent, ComponentStartup args)
    {
        List<EntityUid> invalidSources = new();
        foreach (var sourceUid in sinkComponent.LinkedSources)
        {
            if (!TryComp<DeviceLinkSourceComponent>(sourceUid, out var sourceComponent))
            {
                invalidSources.Add(sourceUid);
                continue;
            }

            if (!sourceComponent.LinkedPorts.TryGetValue(sinkUid, out var linkedPorts))
            {
                foreach (var savedSinks in sourceComponent.Outputs.Values)
                {
                    savedSinks.Remove(sinkUid);
                }
                continue;
            }

            if (sinkComponent.Ports == null)
                continue;

            List<(string, string)> invalidLinks = new();
            foreach (var link in linkedPorts)
            {
                if (!sinkComponent.Ports.Contains(link.sink) || !(sourceComponent.Outputs.GetValueOrDefault(link.source)?.Contains(sinkUid) ?? false))
                    invalidLinks.Add(link);
            }

            foreach (var invalidLink in invalidLinks)
            {
                linkedPorts.Remove(invalidLink);
                sourceComponent.Outputs.GetValueOrDefault(invalidLink.Item1)?.Remove(sinkUid);
            }
        }

        foreach (var invalidSource in invalidSources)
        {
            sinkComponent.LinkedSources.Remove(invalidSource);
        }
    }
    #endregion

    private void OnSourceRemoved(EntityUid uid, DeviceLinkSourceComponent component, ComponentRemove args)
    {
        foreach (var sinkUid in component.LinkedPorts.Keys)
        {
            RemoveSinkFromSource(uid, sinkUid, component);
        }
    }

    private void OnSinkRemoved(EntityUid sinkUid, DeviceLinkSinkComponent sinkComponent, ComponentRemove args)
    {
        foreach (var linkedSource in sinkComponent.LinkedSources)
        {
            RemoveSinkFromSource(linkedSource, sinkUid, null, sinkComponent);
        }
    }

    #region Sending & Receiving
    /// <summary>
    /// Sends a network payload directed at the sink entity.
    /// Just raises a <see cref="SignalReceivedEvent"/> without data if the sink doesn't have a <see cref="DeviceNetworkComponent"/>
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

                //Just skip using device networking if the sink doesn't support it
                if (!TryComp(sinkUid, out DeviceNetworkComponent? sinkNetworkComponent))
                {
                    RaiseLocalEvent(sinkUid, new SignalReceivedEvent(sink));
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

        RaiseLocalEvent(uid, new SignalReceivedEvent(port, args.Data));
    }
    #endregion

    #region Ports
    /// <summary>
    ///     Convenience function to add several ports to an entity.
    /// </summary>
    public void EnsureSourcePorts(EntityUid uid, params string[] ports)
    {
        var comp = EnsureComp<DeviceLinkSourceComponent>(uid);
        comp.Ports ??= new HashSet<string>();

        foreach (var port in ports)
        {
            comp.Ports?.Add(port);
        }
    }

    public void EnsureSinkPorts(EntityUid uid, params string[] ports)
    {
        var comp = EnsureComp<DeviceLinkSinkComponent>(uid);
        comp.Ports ??= new HashSet<string>();

        foreach (var port in ports)
        {
            comp.Ports?.Add(port);
        }
    }

    public List<SourcePortPrototype> GetSourcePorts(EntityUid sourceUid, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || sourceComponent.Ports == null)
            return new List<SourcePortPrototype>();

        var sourcePorts = new List<SourcePortPrototype>();
        foreach (var port in sourceComponent.Ports)
        {
            sourcePorts.Add(_prototypeManager.Index<SourcePortPrototype>(port));
        }

        return sourcePorts;
    }

    public List<SinkPortPrototype> GetSinkPorts(EntityUid sinkUid, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sinkUid, ref sinkComponent) || sinkComponent.Ports == null)
            return new List<SinkPortPrototype>();

        var sinkPorts = new List<SinkPortPrototype>();
        foreach (var port in sinkComponent.Ports)
        {
            sinkPorts.Add(_prototypeManager.Index<SinkPortPrototype>(port));
        }

        return sinkPorts;
    }

    /// <summary>
    ///     Convenience function to retrieve the name of a port prototype.
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public string PortName<TPort>(string port) where TPort : DevicePortPrototype, IPrototype
    {
        if (!_prototypeManager.TryIndex<TPort>(port, out var proto))
            return port;

        return Loc.GetString(proto.Name);
    }
    #endregion

    #region Links
    public HashSet<(string source, string sink)> GetLinks(EntityUid sourceUid, EntityUid sinkUid, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !sourceComponent.LinkedPorts.TryGetValue(sinkUid, out var links))
            return new HashSet<(string source, string sink)>();

        return links;
    }

    public List<(string source, string sink)> GetDefaults(List<SourcePortPrototype> sources)
    {
        var defaults = new List<(string, string)>();
        foreach (var source in sources)
        {
            if (source.DefaultLinks == null)
                return new List<(string, string)>();

            foreach (var defaultLink in source.DefaultLinks)
            {
                defaults.Add((source.ID, defaultLink));
            }
        }

        return defaults;
    }

    public void LinkDefaults(EntityUid? userId, EntityUid sourceUid, EntityUid sinkUid, DeviceLinkSourceComponent? sourceComponent = null, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
            return;

        var sourcePorts = GetSourcePorts(sourceUid, sourceComponent);
        var defaults = GetDefaults(sourcePorts);
        SaveLinks(userId, sourceUid, sinkUid, defaults, sourceComponent, sinkComponent);

        if (userId != null)
            _popupSystem.PopupCursor(Loc.GetString("signal-linking-verb-success", ("machine", sourceUid)), userId.Value);
    }

    /// <summary>
    /// Saves multiple links between a source and a sink device.
    /// Ignores links where either the source or sink port aren't present
    /// </summary>
    public void SaveLinks(EntityUid? userId, EntityUid sourceUid, EntityUid sinkUid, List<(string source, string sink)> links,
        DeviceLinkSourceComponent? sourceComponent = null, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
            return;

        if (sourceComponent.Ports == null || sinkComponent.Ports == null)
            return;

        if (!InRange(sourceUid, sinkUid, sourceComponent.Range))
        {
            if (userId != null)
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"), userId.Value);

            return;
        }

        RemoveSinkFromSource(sourceUid, sinkUid, sourceComponent);
        foreach (var (source, sink) in links)
        {
            if (!sourceComponent.Ports.Contains(source) || !sinkComponent.Ports.Contains(sink))
                continue;

            if (!CanLink(userId, sourceUid, sinkUid, source, sink, false, sourceComponent))
                continue;

            sourceComponent.Outputs.GetOrNew(source).Add(sinkUid);
            sourceComponent.LinkedPorts.GetOrNew(sinkUid).Add((source, sink));

            SendNewLinkEvent(userId, sourceUid, source, sinkUid, sink);
        }

        if (links.Count > 0)
            sinkComponent.LinkedSources.Add(sourceUid);
    }

    public void RemoveSinkFromSource(EntityUid sourceUid, EntityUid sinkUid,
        DeviceLinkSourceComponent? sourceComponent = null, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
            return;

        if (sourceComponent.LinkedPorts.TryGetValue(sinkUid, out var ports))
        {
            foreach (var (sourcePort, sinkPort) in ports)
            {
                RaiseLocalEvent(sourceUid, new PortDisconnectedEvent(sourcePort));
                RaiseLocalEvent(sinkUid, new PortDisconnectedEvent(sinkPort));
            }
        }

        sinkComponent.LinkedSources.Remove(sourceUid);
        sourceComponent.LinkedPorts.Remove(sinkUid);
        var outputLists = sourceComponent.Outputs.Values;
        foreach (var outputList in outputLists)
        {
            outputList.Remove(sinkUid);
        }

    }

    /// <summary>
    /// Adds or removes a link depending on if it's already present
    /// </summary>
    /// <returns>True if the link was successfully added or removed</returns>
    public bool ToggleLink(EntityUid? userId, EntityUid sourceUid, EntityUid sinkUid, string source, string sink,
        DeviceLinkSourceComponent? sourceComponent = null, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
            return false;

        if (sourceComponent.Ports == null || sinkComponent.Ports == null)
            return false;

        var outputs = sourceComponent.Outputs.GetOrNew(source);
        var linkedPorts = sourceComponent.LinkedPorts.GetOrNew(sinkUid);

        if (linkedPorts.Contains((source, sink)))
        {
            RaiseLocalEvent(sourceUid, new PortDisconnectedEvent(source));
            RaiseLocalEvent(sinkUid, new PortDisconnectedEvent(sink));

            outputs.Remove(sinkUid);
            linkedPorts.Remove((source, sink));

            if (linkedPorts.Count != 0)
                return true;

            sourceComponent.LinkedPorts.Remove(sinkUid);
            sinkComponent.LinkedSources.Remove(sourceUid);
            CreateLinkPopup(userId, sourceUid, source, sinkUid, sink, true);
        }
        else
        {
            if (!sourceComponent.Ports.Contains(source) || !sinkComponent.Ports.Contains(sink))
                return false;

            if (!CanLink(userId, sourceUid, sinkUid, source, sink, true, sourceComponent))
                return false;

            outputs.Add(sinkUid);
            linkedPorts.Add((source, sink));
            sinkComponent.LinkedSources.Add(sourceUid);

            SendNewLinkEvent(userId, sourceUid, source, sinkUid, sink);
            CreateLinkPopup(userId, sourceUid, source, sinkUid, sink, false);
        }

        return true;
    }

    private bool CanLink(EntityUid? userId, EntityUid sourceUid, EntityUid sinkUid, string source, string sink,
        bool checkRange = true, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent))
            return false;

        if (checkRange && !InRange(sourceUid, sinkUid, sourceComponent.Range))
        {
            if (userId.HasValue)
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"), userId.Value);

            return false;
        }

        var linkAttemptEvent = new LinkAttemptEvent(userId, sourceUid, source, sinkUid, sink);

        RaiseLocalEvent(sourceUid, linkAttemptEvent, true);
        if (linkAttemptEvent.Cancelled && userId.HasValue)
        {
            _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", source)), userId.Value);
            return false;
        }

        RaiseLocalEvent(sinkUid, linkAttemptEvent, true);
        if (linkAttemptEvent.Cancelled && userId.HasValue)
        {
            _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", source)), userId.Value);
            return false;
        }

        return !linkAttemptEvent.Cancelled;
    }

    private bool InRange(EntityUid sourceUid, EntityUid sinkUid, float range, ApcPowerReceiverComponent? sourcePowerReceiver = null, ApcPowerReceiverComponent? sinkPowerReceiver = null)
    {
        if (Resolve(sourceUid, ref sourcePowerReceiver) && Resolve(sinkUid, ref sinkPowerReceiver) && sourcePowerReceiver.Provider?.Net == sinkPowerReceiver.Provider?.Net)
            return false;

        return Transform(sourceUid).MapPosition.InRange(Transform(sinkUid).MapPosition, range);
    }

    private void SendNewLinkEvent(EntityUid? user, EntityUid sourceUid, string source, EntityUid sinkUid, string sink)
    {
        var newLinkEvent = new NewLinkEvent(user, sourceUid, source, sinkUid, sink);
        RaiseLocalEvent(sourceUid, newLinkEvent);
        RaiseLocalEvent(sinkUid, newLinkEvent);
    }

    private void CreateLinkPopup(EntityUid? userId, EntityUid sourceUid, string source, EntityUid sinkUid, string sink, bool removed)
    {
        if (!userId.HasValue)
            return;

        var locString = removed ? "signal-linker-component-unlinked-port" : "signal-linker-component-linked-port";

        _popupSystem.PopupCursor(Loc.GetString(locString, ("machine1", sourceUid), ("port1", PortName<SourcePortPrototype>(source)),
                ("machine2", sinkUid), ("port2", PortName<SinkPortPrototype>(sink))), userId.Value, PopupType.Medium);
    }
    #endregion
}
