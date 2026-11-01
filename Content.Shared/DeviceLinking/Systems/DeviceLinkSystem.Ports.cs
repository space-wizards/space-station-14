using Content.Shared.DeviceLinking.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class DeviceLinkSystem
{
    /// <summary>
    /// Convenience function to add a source port to an entity.
    /// </summary>
    [PublicAPI]
    public void EnsureSourcePort(EntityUid uid, ProtoId<SourcePortPrototype> port)
    {
        var comp = EnsureComp<DeviceLinkSourceComponent>(uid);
        comp.Ports.Add(port);
        Dirty(uid, comp);
    }

    /// <summary>
    /// Convenience function to a sink port to an entity.
    /// </summary>
    [PublicAPI]
    public void EnsureSinkPort(EntityUid uid, ProtoId<SinkPortPrototype> port)
    {
        var comp = EnsureComp<DeviceLinkSinkComponent>(uid);
        comp.Ports.Add(port);
        DirtyField(uid, comp, nameof(DeviceLinkSinkComponent.Ports));
    }

    /// <summary>
    /// Convenience function to add several ports to an entity.
    /// </summary>
    [PublicAPI]
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
    [PublicAPI]
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

    [PublicAPI]
    public ProtoId<SourcePortPrototype>[] GetSourcePortIds(Entity<DeviceLinkSourceComponent> source)
    {
        return [.. source.Comp.Ports];
    }

    /// <summary>
    /// Retrieves the available ports from a source
    /// </summary>
    /// <returns>A list of source port prototypes</returns>
    [PublicAPI]
    public HashSet<ProtoId<SourcePortPrototype>> GetSourcePorts(Entity<DeviceLinkSourceComponent?> source)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp))
            return [];

        return source.Comp.Ports;
    }

    [PublicAPI]
    public ProtoId<SinkPortPrototype>[] GetSinkPortIds(Entity<DeviceLinkSinkComponent> source)
    {
        return [.. source.Comp.Ports];
    }

    /// <summary>
    /// Retrieves the available ports from a sink.
    /// </summary>
    /// <returns>A list of sink port prototypes.</returns>
    [PublicAPI]
    public List<SinkPortPrototype> GetSinkPorts(Entity<DeviceLinkSinkComponent?> sink)
    {
        if (!_deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp))
            return [];

        var sinkPorts = new List<SinkPortPrototype>();
        foreach (var port in sink.Comp.Ports)
        {
            sinkPorts.Add(ProtoMan.Index(port));
        }

        return sinkPorts;
    }

    /// <summary>
    /// Convenience function to retrieve the name of a <see cref="DevicePortPrototype"/>.
    /// </summary>
    [PublicAPI]
    public string PortName<TPort>(string port) where TPort : DevicePortPrototype, IPrototype
    {
        if (!ProtoMan.TryIndex<TPort>(port, out var proto))
            return port;

        return Loc.GetString(proto.Name);
    }
}
