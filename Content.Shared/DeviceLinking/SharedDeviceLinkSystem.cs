using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceLinking;

public abstract class SharedDeviceLinkSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public const string InvokedPort = "link_port";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceLinkSourceComponent, ComponentStartup>(OnSourceStartup);
        SubscribeLocalEvent<DeviceLinkSourceComponent, ComponentRemove>(OnSourceRemoved);
        SubscribeLocalEvent<DeviceLinkSinkComponent, ComponentRemove>(OnSinkRemoved);
    }

    #region Link Validation

    /// <summary>
    /// Removes invalid links where the saved sink doesn't exist/have a sink component for example
    /// </summary>
    private void OnSourceStartup(Entity<DeviceLinkSourceComponent> source, ref ComponentStartup args)
    {
        List<EntityUid> invalidSinks = new();
        List<(string, string)> invalidLinks = new();
        foreach (var (sink, links)  in source.Comp.LinkedPorts)
        {
            if (!TryComp(sink, out DeviceLinkSinkComponent? sinkComponent))
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
        }

        foreach (var sink in invalidSinks)
        {
            source.Comp.LinkedPorts.Remove(sink);
            Log.Warning($"Device source {ToPrettyString(source)} contains invalid sink: {ToPrettyString(sink)}");
        }
    }
    #endregion

    /// <summary>
    /// Ensures that its links get deleted when a source gets removed
    /// </summary>
    private void OnSourceRemoved(Entity<DeviceLinkSourceComponent> source, ref ComponentRemove args)
    {
        var query = GetEntityQuery<DeviceLinkSinkComponent>();
        foreach (var sinkUid in source.Comp.LinkedPorts.Keys)
        {
            if (query.TryGetComponent(sinkUid, out var sink))
                RemoveSinkFromSourceInternal(source, sinkUid, source, sink);
            else
                Log.Error($"Device source {ToPrettyString(source)} links to invalid entity: {ToPrettyString(sinkUid)}");
        }
    }

    /// <summary>
    /// Ensures that its links get deleted when a sink gets removed
    /// </summary>
    private void OnSinkRemoved(Entity<DeviceLinkSinkComponent> sink, ref ComponentRemove args)
    {
        foreach (var sourceUid in sink.Comp.LinkedSources)
        {
            if (TryComp(sourceUid, out DeviceLinkSourceComponent? source))
                RemoveSinkFromSourceInternal(sourceUid, sink, source, sink);
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
            if (!_prototypeManager.HasIndex(port))
                Log.Error($"Attempted to add invalid port {port} to {ToPrettyString(uid)}");
            else
                comp.Ports.Add(port);
        }
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
            if (!_prototypeManager.HasIndex(port))
                Log.Error($"Attempted to add invalid port {port} to {ToPrettyString(uid)}");
            else
                comp.Ports.Add(port);
        }
    }

    /// <summary>
    /// Retrieves the available ports from a source
    /// </summary>
    /// <returns>A list of source port prototypes</returns>
    public List<SourcePortPrototype> GetSourcePorts(EntityUid sourceUid, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent))
            return new List<SourcePortPrototype>();

        var sourcePorts = new List<SourcePortPrototype>();
        foreach (var port in sourceComponent.Ports)
        {
            sourcePorts.Add(_prototypeManager.Index(port));
        }

        return sourcePorts;
    }

    /// <summary>
    /// Retrieves the available ports from a sink
    /// </summary>
    /// <returns>A list of sink port prototypes</returns>
    public List<SinkPortPrototype> GetSinkPorts(EntityUid sinkUid, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sinkUid, ref sinkComponent))
            return new List<SinkPortPrototype>();

        var sinkPorts = new List<SinkPortPrototype>();
        foreach (var port in sinkComponent.Ports)
        {
            sinkPorts.Add(_prototypeManager.Index(port));
        }

        return sinkPorts;
    }

    /// <summary>
    /// Convenience function to retrieve the name of a port prototype
    /// </summary>
    public string PortName<TPort>(string port) where TPort : DevicePortPrototype, IPrototype
    {
        if (!_prototypeManager.TryIndex<TPort>(port, out var proto))
            return port;

        return Loc.GetString(proto.Name);
    }
    #endregion

    #region Links
    /// <summary>
    /// Returns the links of a source
    /// </summary>
    /// <returns>A list of sink and source port ids that are linked together</returns>
    public HashSet<(ProtoId<SourcePortPrototype> source, ProtoId<SinkPortPrototype> sink)> GetLinks(EntityUid sourceUid, EntityUid sinkUid, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !sourceComponent.LinkedPorts.TryGetValue(sinkUid, out var links))
            return new HashSet<(ProtoId<SourcePortPrototype>, ProtoId<SinkPortPrototype>)>();

        return links;
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
    /// <param name="userId">Optinal user uid for displaying popups</param>
    /// <param name="sourceUid">The source uid</param>
    /// <param name="sinkUid">The sink uid</param>
    /// <param name="sourceComponent"></param>
    /// <param name="sinkComponent"></param>
    public void LinkDefaults(
        EntityUid? userId,
        EntityUid sourceUid,
        EntityUid sinkUid,
        DeviceLinkSourceComponent? sourceComponent = null,
        DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
            return;

        if (userId != null)
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userId.Value):actor} is linking defaults between {ToPrettyString(sourceUid):source} and {ToPrettyString(sinkUid):sink}");
        else
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"linking defaults between {ToPrettyString(sourceUid):source} and {ToPrettyString(sinkUid):sink}");

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
    /// <param name="userId">Optinal user uid for displaying popups</param>
    /// <param name="sourceUid">The source uid</param>
    /// <param name="sinkUid">The sink uid</param>
    /// <param name="links">List of source and sink ids to link</param>
    /// <param name="sourceComponent"></param>
    /// <param name="sinkComponent"></param>
    public void SaveLinks(
        EntityUid? userId,
        EntityUid sourceUid,
        EntityUid sinkUid,
        List<(string source, string sink)> links,
        DeviceLinkSourceComponent? sourceComponent = null,
        DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
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
            DebugTools.Assert(_prototypeManager.HasIndex<SourcePortPrototype>(source));
            DebugTools.Assert(_prototypeManager.HasIndex<SinkPortPrototype>(sink));

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

    /// <summary>
    /// Removes every link from the given sink
    /// </summary>
    public void RemoveAllFromSink(EntityUid sinkUid, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sinkUid, ref sinkComponent))
            return;

        foreach (var sourceUid in sinkComponent.LinkedSources)
        {
            RemoveSinkFromSource(sourceUid, sinkUid, null, sinkComponent);
        }
    }

    /// <summary>
    /// Removes all links between a source and a sink
    /// </summary>
    public void RemoveSinkFromSource(
        EntityUid sourceUid,
        EntityUid sinkUid,
        DeviceLinkSourceComponent? sourceComponent = null,
        DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (Resolve(sourceUid, ref sourceComponent, false) && Resolve(sinkUid, ref sinkComponent, false))
        {
            RemoveSinkFromSourceInternal(sourceUid, sinkUid, sourceComponent, sinkComponent);
            return;
        }

        if (sourceComponent == null && sinkComponent == null)
        {
            // Both were deleted?
            return;
        }

        if (sourceComponent == null)
        {
            Log.Error($"Attempted to remove link between {ToPrettyString(sourceUid)} and {ToPrettyString(sinkUid)}, but the source component was missing.");
            sinkComponent!.LinkedSources.Remove(sourceUid);
        }
        else
        {
            Log.Error($"Attempted to remove link between {ToPrettyString(sourceUid)} and {ToPrettyString(sinkUid)}, but the sink component was missing.");
            sourceComponent.LinkedPorts.Remove(sinkUid);
        }
    }

    private void RemoveSinkFromSourceInternal(
        EntityUid sourceUid,
        EntityUid sinkUid,
        DeviceLinkSourceComponent sourceComponent,
        DeviceLinkSinkComponent sinkComponent)
    {
        // This function gets called on component removal. Beware that TryComp & Resolve may return false.

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
        foreach (var outputList in sourceComponent.Outputs.Values)
        {
            outputList.Remove(sinkUid);
        }
    }

    /// <summary>
    /// Adds or removes a link depending on if it's already present
    /// </summary>
    /// <returns>True if the link was successfully added or removed</returns>
    public bool ToggleLink(
        EntityUid? userId,
        EntityUid sourceUid,
        EntityUid sinkUid,
        string source,
        string sink,
        DeviceLinkSourceComponent? sourceComponent = null,
        DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
            return false;

        var outputs = sourceComponent.Outputs.GetOrNew(source);
        var linkedPorts = sourceComponent.LinkedPorts.GetOrNew(sinkUid);

        if (linkedPorts.Contains((source, sink)))
        {
            if (userId != null)
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userId.Value):actor} unlinked {ToPrettyString(sourceUid):source} {source} and {ToPrettyString(sinkUid):sink} {sink}");
            else
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"unlinked {ToPrettyString(sourceUid):source} {source} and {ToPrettyString(sinkUid):sink} {sink}");

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

    /// <summary>
    /// Checks if a source and a sink can be linked by allowing other systems to veto the link
    /// and by optionally checking if they are in range of each other
    /// </summary>
    /// <returns></returns>
    private bool CanLink(
        EntityUid? userId,
        EntityUid sourceUid,
        EntityUid sinkUid,
        string source,
        string sink,
        bool checkRange = true,
        DeviceLinkSourceComponent? sourceComponent = null)
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

    #region Sending & Receiving
    /// <summary>
    /// Sends a network payload directed at the sink entity.
    /// Just raises a <see cref="SignalReceivedEvent"/> without data if the source or the sink doesn't have a <see cref="DeviceNetworkComponent"/>
    /// </summary>
    /// <param name="uid">The source uid that invokes the port</param>
    /// <param name="port">The port to invoke</param>
    /// <param name="data">Optional data to send along</param>
    /// <param name="sourceComponent"></param>
    public virtual void InvokePort(EntityUid uid, string port, NetworkPayload? data = null,
        DeviceLinkSourceComponent? sourceComponent = null)
    {
        // NOOP on client for the moment.
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

    protected void SetInvokeCounter(DeviceLinkSinkComponent sink, int value)
    {
        sink.InvokeCounterTick = _gameTiming.CurTick;
        sink.InvokeCounter = value;
    }
}
