using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Popups;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceLinking;

public sealed partial class DeviceLinkSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    [Dependency] private EntityQuery<DeviceLinkSinkComponent> _deviceLinkSinkQuery = default!;
    [Dependency] private EntityQuery<DeviceLinkSourceComponent> _deviceLinkSourceQuery = default!;
    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery = default!;

    [SubscribeLocalEvent]
    private void OnGetState(Entity<DeviceLinkSourceComponent> ent, ref ComponentGetState args)
    {
        var netOutputs = new Dictionary<ProtoId<SourcePortPrototype>, HashSet<NetEntity>>(ent.Comp.Outputs.Count);
        foreach (var (key, output) in ent.Comp.Outputs)
        {
            var set = GetNetEntitySet(output);
            netOutputs.Add(key, set);
        }

        args.State = new DeviceLinkSourceComponentState(netOutputs, ent.Comp.LastSignals, GetNetEntityDictionary(ent.Comp.LinkedPorts));
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<DeviceLinkSourceComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DeviceLinkSourceComponentState state)
            return;

        var outputs = new Dictionary<ProtoId<SourcePortPrototype>, HashSet<EntityUid>>(state.Outputs.Count);
        foreach (var (key, output) in state.Outputs)
        {
            var netSet = GetEntitySet(output);
            var set = new HashSet<EntityUid>(netSet.Count);
            foreach (var uid in netSet)
            {
                if (Exists(uid) && !TerminatingOrDeleted(uid))
                    set.Add(uid);
            }

            outputs.Add(key, set);
        }

        var linked = new Dictionary<EntityUid, HashSet<(ProtoId<SourcePortPrototype> Source, ProtoId<SinkPortPrototype> Sink)>>(state.LinkedPorts.Count);
        foreach (var (net, value) in state.LinkedPorts)
        {
            if (TryGetEntity(net, out var uid))
                linked.Add(uid.Value, value);
        }

        ent.Comp.Outputs = outputs;
        ent.Comp.LinkedPorts = linked;
        ent.Comp.LastSignals = state.LastSignals;
    }

    #region Link Validation

    /// <summary>
    /// Removes invalid links where the saved sink doesn't exist/have a sink component for example
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSourceStartup(Entity<DeviceLinkSourceComponent> source, ref ComponentStartup args)
    {
        ValueList<EntityUid> invalidSinks = new(source.Comp.LinkedPorts.Count);
        ValueList<(string, string)> invalidLinks = new(source.Comp.LinkedPorts.Count);
        foreach (var (sink, links)  in source.Comp.LinkedPorts)
        {
            if (!_deviceLinkSinkQuery.TryComp(sink, out var sinkComponent))
            {
                invalidSinks.Add(sink);
                continue;
            }

            foreach (var link in links)
            {
                if (sinkComponent.Ports.Contains(link.Sink) && source.Comp.Ports.Contains(link.Source))
                    source.Comp.Outputs.GetOrNew(link.Source).Add(sink);
                else
                    invalidLinks.Add(link);
            }

            foreach (var link in invalidLinks)
            {
                Log.Warning($"Device source {ToPrettyString(source)} contains invalid links to entity {ToPrettyString(sink)}: {link.Item1}->{link.Item2}");
                links.Remove(link);
            }

            if (links.Count == 0)
            {
                invalidSinks.Add(sink);
                continue;
            }

            invalidLinks.Clear();
            sinkComponent.LinkedSources.Add(source.Owner);
            DirtyField(sink, sinkComponent, nameof(DeviceLinkSinkComponent.LinkedSources));
        }

        foreach (var sink in invalidSinks)
        {
            source.Comp.LinkedPorts.Remove(sink);
            Log.Warning($"Device source {ToPrettyString(source)} contains invalid sink: {ToPrettyString(sink)}");
        }

        Dirty(source);
    }
    #endregion

    /// <summary>
    /// Ensures that its links get deleted when a source gets removed
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSourceRemoved(Entity<DeviceLinkSourceComponent> source, ref ComponentRemove args)
    {
        foreach (var sinkUid in source.Comp.LinkedPorts.Keys)
        {
            if (_deviceLinkSinkQuery.TryGetComponent(sinkUid, out var sink))
                RemoveSinkFromSourceInternal(source, (sinkUid, sink));
            else
                Log.Error($"Device source {ToPrettyString(source)} links to invalid entity: {ToPrettyString(sinkUid)}");
        }
    }

    /// <summary>
    /// Ensures that its links get deleted when a sink gets removed
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSinkRemoved(Entity<DeviceLinkSinkComponent> sink, ref ComponentRemove args)
    {
        foreach (var sourceUid in sink.Comp.LinkedSources)
        {
            if (_deviceLinkSourceQuery.TryComp(sourceUid, out var source))
                RemoveSinkFromSourceInternal((sourceUid, source), sink);
            else
                Log.Error($"Device sink {ToPrettyString(sink)} source list contains invalid entity: {ToPrettyString(sourceUid)}");
        }
    }

    #region Ports
    /// <summary>
    /// Convenience function to add several ports to an entity
    /// </summary>
    public void EnsureSourcePorts(EntityUid uid, params ProtoId<SourcePortPrototype>[] ports)
    {
        if (ports.Length == 0)
            return;

        var comp = EnsureComp<DeviceLinkSourceComponent>(uid);
        foreach (var port in ports)
        {
            if (!ProtoMan.HasIndex(port))
                Log.Error($"Attempted to add invalid port {port} to {ToPrettyString(uid)}");
            else
                comp.Ports.Add(port);
        }
        Dirty(uid, comp);
    }

    /// <summary>
    /// Convenience function to add several ports to an entity.
    /// </summary>
    public void EnsureSinkPorts(EntityUid uid, params ProtoId<SinkPortPrototype>[] ports)
    {
        if (ports.Length == 0)
            return;

        var comp = EnsureComp<DeviceLinkSinkComponent>(uid);
        foreach (var port in ports)
        {
            if (!ProtoMan.HasIndex(port))
                Log.Error($"Attempted to add invalid port {port} to {ToPrettyString(uid)}");
            else
                comp.Ports.Add(port);
        }
        Dirty(uid, comp);
    }

    public ProtoId<SourcePortPrototype>[] GetSourcePortIds(Entity<DeviceLinkSourceComponent> source)
    {
        return source.Comp.Ports.ToArray();
    }

    /// <summary>
    /// Retrieves the available ports from a source
    /// </summary>
    /// <returns>A list of source port prototypes</returns>
    public List<SourcePortPrototype> GetSourcePorts(Entity<DeviceLinkSourceComponent?> source)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp))
            return new List<SourcePortPrototype>();

        var sourcePorts = new List<SourcePortPrototype>();
        foreach (var port in source.Comp.Ports)
        {
            sourcePorts.Add(ProtoMan.Index(port));
        }

        return sourcePorts;
    }

    public ProtoId<SinkPortPrototype>[] GetSinkPortIds(Entity<DeviceLinkSinkComponent> source)
    {
        return source.Comp.Ports.ToArray();
    }

    /// <summary>
    /// Retrieves the available ports from a sink
    /// </summary>
    /// <returns>A list of sink port prototypes</returns>
    public List<SinkPortPrototype> GetSinkPorts(Entity<DeviceLinkSinkComponent?> sink)
    {
        if (!_deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp))
            return new List<SinkPortPrototype>();

        var sinkPorts = new List<SinkPortPrototype>();
        foreach (var port in sink.Comp.Ports)
        {
            sinkPorts.Add(ProtoMan.Index(port));
        }

        return sinkPorts;
    }

    /// <summary>
    /// Convenience function to retrieve the name of a port prototype
    /// </summary>
    public string PortName<TPort>(string port) where TPort : DevicePortPrototype, IPrototype
    {
        if (!ProtoMan.TryIndex<TPort>(port, out var proto))
            return port;

        return Loc.GetString(proto.Name);
    }
    #endregion

    #region Links
    /// <summary>
    /// Returns the links of a source
    /// </summary>
    /// <returns>A list of sink and source port ids that are linked together</returns>
    public HashSet<(ProtoId<SourcePortPrototype> source, ProtoId<SinkPortPrototype> sink)> GetLinks(Entity<DeviceLinkSourceComponent?> source, EntityUid sinkUid)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp) || !source.Comp.LinkedPorts.TryGetValue(sinkUid, out var links))
            return new HashSet<(ProtoId<SourcePortPrototype>, ProtoId<SinkPortPrototype>)>();

        return links;
    }

    /// <summary>
    /// Gets the entities linked to a specific source port.
    /// </summary>
    public HashSet<EntityUid> GetLinkedSinks(Entity<DeviceLinkSourceComponent?> source, ProtoId<SourcePortPrototype> port)
    {
        if (!_deviceLinkSourceQuery.Resolve(source, ref source.Comp) || !source.Comp.Outputs.TryGetValue(port, out var linked))
            return new HashSet<EntityUid>(); // not a source or not linked

        return new HashSet<EntityUid>(linked); // clone to prevent modifying the original
    }

    /// <summary>
    /// Returns the default links for the given list of source port prototypes
    /// </summary>
    /// <param name="sources">The list of source port prototypes to get the default links for</param>
    /// <returns>A list of sink and source port ids</returns>
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

    /// <summary>
    /// Links the given source and sink by their default links
    /// </summary>
    public void LinkDefaults(
        EntityUid? userId,
        Entity<DeviceLinkSourceComponent?> source,
        Entity<DeviceLinkSinkComponent?> sink)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp, false)
            || !_deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp, false))
            return;

        if (userId != null)
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userId.Value):actor} is linking defaults between {ToPrettyString(source):source} and {ToPrettyString(sink):sink}");
        else
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"linking defaults between {ToPrettyString(source):source} and {ToPrettyString(sink):sink}");

        var sourcePorts = GetSourcePorts(source);
        var defaults = GetDefaults(sourcePorts);
        SaveLinks(userId, source, sink, defaults);

        if (userId != null)
            _popupSystem.PopupCursor(Loc.GetString("signal-linking-verb-success", ("machine", source)), userId.Value);
    }


    /// <summary>
    /// Saves multiple links between a source and a sink device.
    /// Ignores links where either the source or sink port aren't present
    /// </summary>
    public void SaveLinks(
        EntityUid? userId,
        Entity<DeviceLinkSourceComponent?> source,
        Entity<DeviceLinkSinkComponent?> sink,
        List<(string source, string sink)> links)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp, false)
            || !_deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp, false))
            return;

        if (!InRange(source, sink, source.Comp.Range))
        {
            if (userId != null)
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"), userId.Value);

            return;
        }

        RemoveSinkFromSource(source, sink);
        foreach (var (sourcePort, sinkPort) in links)
        {
            DebugTools.Assert(ProtoMan.HasIndex<SourcePortPrototype>(sourcePort));
            DebugTools.Assert(ProtoMan.HasIndex<SinkPortPrototype>(sinkPort));

            if (!source.Comp.Ports.Contains(sourcePort) || !sink.Comp.Ports.Contains(sinkPort))
                continue;

            if (!CanLink(userId, source, sink, sourcePort, sinkPort, false))
                continue;

            source.Comp.Outputs.GetOrNew(sourcePort).Add(sink);
            source.Comp.LinkedPorts.GetOrNew(sink).Add((sourcePort, sinkPort));

            SendNewLinkEvent(userId, source, sourcePort, sink, sinkPort);
            Dirty(source);
        }

        if (links.Count > 0)
            sink.Comp.LinkedSources.Add(source);

        DirtyField(sink, nameof(DeviceLinkSinkComponent.LinkedSources));
    }

    /// <summary>
    /// Removes every link from the given sink
    /// </summary>
    public void RemoveAllFromSink(Entity<DeviceLinkSinkComponent?> sink)
    {
        if (!_deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp))
            return;

        foreach (var sourceUid in sink.Comp.LinkedSources)
        {
            RemoveSinkFromSource(sourceUid, sink);
        }
    }

    /// <summary>
    /// Removes all links between a source and a sink
    /// </summary>
    public void RemoveSinkFromSource(
        Entity<DeviceLinkSourceComponent?> source,
        Entity<DeviceLinkSinkComponent?> sink)
    {
        if (_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp, false)
            && _deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp, false))
        {
            RemoveSinkFromSourceInternal(source!, sink!);
            return;
        }

        if (source.Comp == null && sink.Comp == null)
        {
            // Both were deleted?
            return;
        }

        if (source.Comp == null)
        {
            Log.Error($"Attempted to remove link between {ToPrettyString(source)} and {ToPrettyString(sink)}, but the source component was missing.");
            sink.Comp!.LinkedSources.Remove(source);
            DirtyField(sink, nameof(DeviceLinkSinkComponent.LinkedSources));
        }
        else
        {
            Log.Error($"Attempted to remove link between {ToPrettyString(source)} and {ToPrettyString(sink)}, but the sink component was missing.");
            source.Comp.LinkedPorts.Remove(sink);
            Dirty(source);
        }
    }

    private void RemoveSinkFromSourceInternal(
        Entity<DeviceLinkSourceComponent> source,
        Entity<DeviceLinkSinkComponent> sink)
    {
        // This function gets called on component removal. Beware that TryComp & Resolve may return false.

        if (source.Comp.LinkedPorts.TryGetValue(sink, out var ports))
        {
            foreach (var (sourcePort, sinkPort) in ports)
            {
                var sourceEv = new PortDisconnectedEvent(sourcePort);
                var sinkEv = new PortDisconnectedEvent(sinkPort);
                RaiseLocalEvent(source, ref sourceEv);
                RaiseLocalEvent(sink, ref sinkEv);
            }
        }

        sink.Comp.LinkedSources.Remove(source);
        source.Comp.LinkedPorts.Remove(sink);
        foreach (var outputList in source.Comp.Outputs.Values)
        {
            outputList.Remove(sink);
        }

        DirtyField(sink, sink.Comp, nameof(DeviceLinkSinkComponent.LinkedSources));
        Dirty(source);
    }

    /// <summary>
    /// Adds or removes a link depending on if it's already present
    /// </summary>
    /// <returns>True if the link was successfully added or removed</returns>
    public bool ToggleLink(
        EntityUid? userId,
        Entity<DeviceLinkSourceComponent?> source,
        Entity<DeviceLinkSinkComponent?> sink,
        string sourcePort,
        string sinkPort)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp)
            || !_deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp))
            return false;

        var outputs = source.Comp.Outputs.GetOrNew(sourcePort);
        var linkedPorts = source.Comp.LinkedPorts.GetOrNew(sink);

        if (linkedPorts.Contains((sourcePort, sinkPort)))
        {
            if (userId != null)
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userId.Value):actor} unlinked {ToPrettyString(source):source} {sourcePort} and {ToPrettyString(sink):sink} {sinkPort}");
            else
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"unlinked {ToPrettyString(source):source} {sourcePort} and {ToPrettyString(sink):sink} {sinkPort}");

            var sourceEv = new PortDisconnectedEvent(sourcePort);
            var sinkEv = new PortDisconnectedEvent(sinkPort);
            RaiseLocalEvent(source, ref sourceEv);
            RaiseLocalEvent(sink, ref sinkEv);

            outputs.Remove(sink);
            linkedPorts.Remove((sourcePort, sinkPort));

            if (linkedPorts.Count != 0)
                return true;

            source.Comp.LinkedPorts.Remove(sink);
            sink.Comp.LinkedSources.Remove(source);
            CreateLinkPopup(userId, source, sourcePort, sink, sinkPort, true);
        }
        else
        {
            if (!source.Comp.Ports.Contains(sourcePort) || !sink.Comp.Ports.Contains(sinkPort))
                return false;

            if (!CanLink(userId, source, sink, sourcePort, sinkPort))
                return false;

            outputs.Add(sink);
            linkedPorts.Add((sourcePort, sinkPort));
            sink.Comp.LinkedSources.Add(source);

            SendNewLinkEvent(userId, source, sourcePort, sink, sinkPort);
            CreateLinkPopup(userId, source, sourcePort, sink, sinkPort, false);
        }

        DirtyField(sink, nameof(DeviceLinkSinkComponent.LinkedSources));
        Dirty(source);
        return true;
    }

    /// <summary>
    /// Checks if a source and a sink can be linked by allowing other systems to veto the link
    /// and by optionally checking if they are in range of each other
    /// </summary>
    /// <returns></returns>
    private bool CanLink(
        EntityUid? userId,
        Entity<DeviceLinkSourceComponent?> source,
        EntityUid sinkUid,
        string sourcePort,
        string sinkPort,
        bool checkRange = true)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp))
            return false;

        if (checkRange && !InRange(source, sinkUid, source.Comp.Range))
        {
            if (userId.HasValue)
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"), userId.Value);

            return false;
        }

        var linkAttemptEvent = new LinkAttemptEvent(userId, source, sourcePort, sinkUid, sinkPort);

        RaiseLocalEvent(source, ref linkAttemptEvent, true);
        if (linkAttemptEvent.Cancelled && userId.HasValue)
        {
            _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", sourcePort)), userId.Value);
            return false;
        }

        RaiseLocalEvent(sinkUid, ref linkAttemptEvent, true);
        if (linkAttemptEvent.Cancelled && userId.HasValue)
        {
            _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", sourcePort)), userId.Value);
            return false;
        }

        return !linkAttemptEvent.Cancelled;
    }

    private bool InRange(EntityUid sourceUid, EntityUid sinkUid, float range)
    {
        // TODO: This should be using an existing method and also coordinates inrange instead.
        return _transform.GetMapCoordinates(sourceUid).InRange(_transform.GetMapCoordinates(sinkUid), range);
    }

    private void SendNewLinkEvent(EntityUid? user, EntityUid sourceUid, string source, EntityUid sinkUid, string sink)
    {
        if (user != null)
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(user.Value):actor} linked {ToPrettyString(sourceUid):source} {source} and {ToPrettyString(sinkUid):sink} {sink}");
        else
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"linked {ToPrettyString(sourceUid):source} {source} and {ToPrettyString(sinkUid):sink} {sink}");

        var newLinkEvent = new NewLinkEvent(user, sourceUid, source, sinkUid, sink);
        RaiseLocalEvent(sourceUid, ref newLinkEvent);
        RaiseLocalEvent(sinkUid, ref newLinkEvent);
    }

    private void CreateLinkPopup(EntityUid? userId, EntityUid sourceUid, string source, EntityUid sinkUid, string sink, bool removed)
    {
        if (!userId.HasValue)
            return;

        var locString = removed ? "signal-linker-component-unlinked-port" : "signal-linker-component-linked-port";

        _popupSystem.PopupCursor(Loc.GetString(locString,
            ("machine1", sourceUid),
            ("port1", PortName<SourcePortPrototype>(source)),
            ("machine2", sinkUid),
            ("port2", PortName<SinkPortPrototype>(sink))),
            userId.Value,
            PopupType.Medium);
    }
    #endregion

    #region Sending & Receiving
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
                    InvokeDirect(ent!, (sinkUid, sinkComponent), source, sink);
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
                    InvokeDirect(ent!, (sinkUid, sinkComponent), source, sink, ref data);
            }
        }
    }

    /// <summary>
    /// Raises an event on or sends a network packet directly to a sink from a source.
    /// </summary>
    private void InvokeDirect<T>(
        Entity<DeviceLinkSourceComponent> source,
        Entity<DeviceLinkSinkComponent?> sink,
        string sourcePort,
        string sinkPort,
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
        _deviceNetworkSystem.QueuePacket(source.Owner, sinkNetwork.Data.Address, ref payload, sinkNetwork.Data.ReceiveFrequency, network);
    }

    /// <summary>
    /// Raises an event on or sends a network packet directly to a sink from a source.
    /// </summary>
    private void InvokeDirect(
        Entity<DeviceLinkSourceComponent> source,
        Entity<DeviceLinkSinkComponent?> sink,
        string sourcePort,
        string sinkPort)
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
        _deviceNetworkSystem.QueuePacket(
            source.Owner,
            sinkNetwork.Data.Address,
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

        var payload = new LogicStatePayload
        {
            State = signal ? SignalState.High : SignalState.Low
        };
        InvokeDirect(ent, args.Sink, args.SourcePort, args.SinkPort, ref payload);
    }
    #endregion

    /// <summary>
    /// Gets how many times a <see cref="DeviceLinkSinkComponent"/> has been invoked recently.
    /// </summary>
    /// <remarks>
    /// The return value of this function goes up by one every time a sink is invoked, and goes down by one every tick.
    /// </remarks>
    public int GetEffectiveInvokeCounter(DeviceLinkSinkComponent sink)
    {
        // Shouldn't be possible but just to be safe.
        var curTick = _gameTiming.CurTick;
        if (curTick < sink.InvokeCounterTick)
            return 0;

        var tickDelta = curTick.Value - sink.InvokeCounterTick.Value;
        if (tickDelta >= sink.InvokeCounter)
            return 0;

        return Math.Max(0, sink.InvokeCounter - (int)tickDelta);
    }

    private void SetInvokeCounter(Entity<DeviceLinkSinkComponent> sink, int value)
    {
        sink.Comp.InvokeCounterTick = _gameTiming.CurTick;
        sink.Comp.InvokeCounter = value;
        DirtyFields(sink.AsNullable(), null, nameof(DeviceLinkSinkComponent.InvokeCounterTick), nameof(DeviceLinkSinkComponent.InvokeCounter));
    }
}
