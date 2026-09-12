using System.Linq;
using Content.Shared.DeviceLinking.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class DeviceLinkSystem
{
    /// <summary>
    /// Convenience function to add a source port to an entity.
    /// </summary>
    public void EnsureSourcePort(EntityUid uid, ProtoId<SourcePortPrototype> port)
    {
        var comp = EnsureComp<DeviceLinkSourceComponent>(uid);
        comp.Ports.Add(port);
        Dirty(uid, comp);
    }

    /// <summary>
    /// Convenience function to a sink port to an entity.
    /// </summary>
    public void EnsureSinkPort(EntityUid uid, ProtoId<SinkPortPrototype> port)
    {
        var comp = EnsureComp<DeviceLinkSinkComponent>(uid);
        comp.Ports.Add(port);
        DirtyField(uid, comp, nameof(DeviceLinkSinkComponent.Ports));
    }

    /// <summary>
    /// Convenience function to add several ports to an entity.
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
    public HashSet<ProtoId<SourcePortPrototype>> GetSourcePorts(Entity<DeviceLinkSourceComponent?> source)
    {
        if (!_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp))
            return new HashSet<ProtoId<SourcePortPrototype>>();

        return source.Comp.Ports;
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
}
