using System.Linq;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class DeviceLinkSystem
{
    /// <summary>
    /// Returns the links of a source
    /// </summary>
    /// <returns>A list of sink and source port ids that are linked together</returns>
    public HashSet<DeviceLink> GetLinks(Entity<DeviceLinkSourceComponent?> source, EntityUid sinkUid)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp) || !source.Comp.LinkedPorts.TryGetValue(sinkUid, out var links))
            return new HashSet<DeviceLink>();

        // TODO fix this when DeviceLinkSourceComponent will store DeviceLinks inside
        return links.Select(x => new DeviceLink(x.Source, x.Sink)).ToHashSet();
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
    public List<DeviceLink> GetDefaults(HashSet<ProtoId<SourcePortPrototype>> sources)
    {
        var defaults = new List<DeviceLink>();
        foreach (var source in sources)
        {
            var proto = ProtoMan.Index(source);
            if (proto.DefaultLinks == null)
                return [];

            foreach (var defaultLink in proto.DefaultLinks)
            {
                defaults.Add(new DeviceLink(source, defaultLink));
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
        List<DeviceLink> links)
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
            source.Comp.LinkedPorts.GetOrNew(sink).Add(new DeviceLink(sourcePort, sinkPort));

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
        ProtoId<SourcePortPrototype> sourcePort,
        ProtoId<SinkPortPrototype> sinkPort)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp)
            || !_deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp))
            return false;

        var outputs = source.Comp.Outputs.GetOrNew(sourcePort);
        var linkedPorts = source.Comp.LinkedPorts.GetOrNew(sink);

        if (linkedPorts.Contains(new DeviceLink(sourcePort, sinkPort)))
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
            linkedPorts.Remove(new DeviceLink(sourcePort, sinkPort));

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
            linkedPorts.Add(new DeviceLink(sourcePort, sinkPort));
            sink.Comp.LinkedSources.Add(source);

            SendNewLinkEvent(userId, source, sourcePort, sink, sinkPort);
            CreateLinkPopup(userId, source, sourcePort, sink, sinkPort, false);
        }

        DirtyField(sink, nameof(DeviceLinkSinkComponent.LinkedSources));
        Dirty(source);
        return true;
    }

    /// <summary>
    /// Adds or removes a link depending on if it's already present
    /// </summary>
    /// <returns>True if the link was successfully added or removed</returns>
    public bool ToggleLink(
        EntityUid? userId,
        Entity<DeviceLinkSourceComponent?> source,
        Entity<DeviceLinkSinkComponent?> sink,
        DeviceLink link)
    {
        return ToggleLink(userId, source, sink, link.SourcePort, link.SinkPort);
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
}
