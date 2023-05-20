using Content.Shared.DeviceLinking.Events;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceLinking;

public abstract class SharedDeviceLinkSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public const string InvokedPort = "link_port";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceLinkSourceComponent, ComponentStartup>(OnSourceStartup);
        SubscribeLocalEvent<DeviceLinkSinkComponent, ComponentStartup>(OnSinkStartup);
        SubscribeLocalEvent<DeviceLinkSourceComponent, ComponentRemove>(OnSourceRemoved);
        SubscribeLocalEvent<DeviceLinkSinkComponent, ComponentRemove>(OnSinkRemoved);
    }

    #region Link Validation
    /// <summary>
    /// Removes invalid links where the saved sink doesn't exist/have a sink component for example
    /// </summary>
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

    /// <summary>
    /// Same with <see cref="OnSourceStartup"/> but also checks that the saved ports are present on the sink
    /// </summary>
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

    /// <summary>
    /// Ensures that its links get deleted when a source gets removed
    /// </summary>
    private void OnSourceRemoved(EntityUid uid, DeviceLinkSourceComponent component, ComponentRemove args)
    {
        foreach (var sinkUid in component.LinkedPorts.Keys)
        {
            RemoveSinkFromSource(uid, sinkUid, component);
        }
    }

    /// <summary>
    /// Ensures that its links get deleted when a sink gets removed
    /// </summary>
    private void OnSinkRemoved(EntityUid sinkUid, DeviceLinkSinkComponent sinkComponent, ComponentRemove args)
    {
        foreach (var linkedSource in sinkComponent.LinkedSources)
        {
            RemoveSinkFromSource(linkedSource, sinkUid, null, sinkComponent);
        }
    }

    #region Ports
    /// <summary>
    /// Convenience function to add several ports to an entity
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

    /// <summary>
    /// Convenience function to add several ports to an entity.
    /// </summary>
    public void EnsureSinkPorts(EntityUid uid, params string[] ports)
    {
        var comp = EnsureComp<DeviceLinkSinkComponent>(uid);
        comp.Ports ??= new HashSet<string>();

        foreach (var port in ports)
        {
            comp.Ports?.Add(port);
        }
    }

    /// <summary>
    /// Retrieves the available ports from a source
    /// </summary>
    /// <returns>A list of source port prototypes</returns>
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

    /// <summary>
    /// Retrieves the available ports from a sink
    /// </summary>
    /// <returns>A list of sink port prototypes</returns>
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
    public HashSet<(string source, string sink)> GetLinks(EntityUid sourceUid, EntityUid sinkUid, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !sourceComponent.LinkedPorts.TryGetValue(sinkUid, out var links))
            return new HashSet<(string source, string sink)>();

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

    /// <summary>
    /// Removes all links between a source and a sink
    /// </summary>
    public void RemoveSinkFromSource(
        EntityUid sourceUid,
        EntityUid sinkUid,
        DeviceLinkSourceComponent? sourceComponent = null,
        DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent, false) || !Resolve(sinkUid, ref sinkComponent, false))
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
