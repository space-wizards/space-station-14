using System.Linq;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.DeviceLinking;

/// <summary>
/// This handles...
/// </summary>
public sealed class DeviceLinkSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceLinkSinkComponent, ComponentRemove>(OnSinkRemoved);
    }

    /// <summary>
    ///     Convenience function to add several ports to an entity.
    /// </summary>
    [Obsolete("Add the ports in yml instead")]
    public void EnsureSourcePorts(EntityUid uid, params string[] ports)
    {
        var comp = EnsureComp<DeviceLinkSourceComponent>(uid);
        foreach (var port in ports)
        {
            comp.Ports?.Add(port);
        }
    }

    [Obsolete("Add the ports in yml instead")]
    public void EnsureSinkPorts(EntityUid uid, params string[] ports)
    {
        var comp = EnsureComp<DeviceLinkSinkComponent>(uid);
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
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"), Filter.Entities(userId.Value));

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

    private bool CanLink(EntityUid? userId, EntityUid sourceUid, EntityUid sinkUid, string source, string sink,
        bool checkRange = true, DeviceLinkSourceComponent? sourceComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent))
            return false;

        var filter = userId.HasValue ? Filter.Entities(userId.Value) : null;

        if (checkRange && !InRange(sourceUid, sinkUid, sourceComponent.Range))
        {
            if (filter != null)
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"), filter);

            return false;
        }

        var linkAttemptEvent = new LinkAttemptEvent(userId, sourceUid, source, sinkUid, sink);

        RaiseLocalEvent(sourceUid, linkAttemptEvent, true);
        if (linkAttemptEvent.Cancelled && filter != null)
        {
            _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", source)), filter);
            return false;
        }

        RaiseLocalEvent(sinkUid, linkAttemptEvent, true);
        if (linkAttemptEvent.Cancelled && filter != null)
        {
            _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", source)), filter);
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
                ("machine2", sinkUid), ("port2", PortName<SinkPortPrototype>(sink))), Filter.Entities(userId.Value), PopupType.Medium);
    }

    private void OnSinkRemoved(EntityUid sinkUid, DeviceLinkSinkComponent sinkComponent, ComponentRemove args)
    {
        foreach (var linkedSource in sinkComponent.LinkedSources)
        {
            RemoveSinkFromSource(linkedSource, sinkUid);
        }
    }

    public void LinkDefaults(EntityUid? userId, EntityUid sourceUid, EntityUid sinkUid, DeviceLinkSourceComponent? sourceComponent = null, DeviceLinkSinkComponent? sinkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent) || !Resolve(sinkUid, ref sinkComponent))
            return;

        var sourcePorts = GetSourcePorts(sourceUid, sourceComponent);
        var defaults = GetDefaults(sourcePorts);
        SaveLinks(userId, sourceUid, sinkUid, defaults, sourceComponent, sinkComponent);

        if (userId != null)
            _popupSystem.PopupCursor(Loc.GetString("signal-linking-verb-success", ("machine", sourceUid)), Filter.Entities(userId.Value));
    }
}
