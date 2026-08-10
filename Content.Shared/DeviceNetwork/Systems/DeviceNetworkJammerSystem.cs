using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

/// <inheritdoc/>
public sealed partial class DeviceNetworkJammerSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    // TODO consider other ways to suppress the packets in the next PR
    // Maybe EntityLookup on a loop + temporarily disconnecting the targeted devices from their network will work faster?
    [SubscribeLocalEvent]
    private void BeforePacketSent(Entity<TransformComponent> xform, ref BeforePacketSentEvent ev)
    {
        if (ev.Cancelled)
            return;

        var query = EntityQueryEnumerator<DeviceNetworkJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var jammerComp, out var jammerXform))
        {
            if (!GetJammableNetworks((uid, jammerComp)).Contains(ev.NetId))
                continue;

            if (jammerComp.FrequenciesExcluded.Contains(ev.Frequency))
                continue;

            if (_transform.InRange(jammerXform.Coordinates, ev.SenderTransform.Coordinates, jammerComp.Range)
                || _transform.InRange(jammerXform.Coordinates, xform.Comp.Coordinates, jammerComp.Range))
            {
                ev.Cancelled = true;
                return;
            }
        }
    }

    /// <summary>
    /// Sets the range of the jamming effect.
    /// </summary>
    public void SetRange(Entity<DeviceNetworkJammerComponent> ent, float value)
    {
        ent.Comp.Range = value;
        DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.Range));
    }

    /// <inheritdoc cref="SetRange"/>
    public bool TrySetRange(Entity<DeviceNetworkJammerComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        SetRange((ent, ent.Comp), value);
        return true;
    }

    /// <summary>
    /// Returns the set of networks that this entity can jam.
    /// </summary>
    public IReadOnlySet<int> GetJammableNetworks(Entity<DeviceNetworkJammerComponent> ent)
    {
        return ent.Comp.JammableNetworks;
    }

    /// <summary>
    /// Enables this entity to jam packets on the specified network.
    /// </summary>
    public void AddJammableNetwork(Entity<DeviceNetworkJammerComponent> ent, int networkId)
    {
        if (ent.Comp.JammableNetworks.Add(networkId))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.JammableNetworks));
    }

    /// <summary>
    /// Stops this entity from jamming packets on the specified network.
    /// </summary>
    public void RemoveJammableNetwork(Entity<DeviceNetworkJammerComponent> ent, int networkId)
    {
        if (ent.Comp.JammableNetworks.Remove(networkId))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.JammableNetworks));
    }

    /// <summary>
    /// Stops this entity from jamming packets on any networks.
    /// </summary>
    public void ClearJammableNetworks(Entity<DeviceNetworkJammerComponent> ent)
    {
        if (ent.Comp.JammableNetworks.Count == 0)
            return;

        ent.Comp.JammableNetworks.Clear();
        DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.JammableNetworks));
    }

    /// <summary>
    /// Enables this entity to stop packets with the specified frequency from being jammmed.
    /// </summary>
    public void AddExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent, DeviceFrequency frequency)
    {
        if (ent.Comp.FrequenciesExcluded.Add(frequency))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.FrequenciesExcluded));
    }

    /// <summary>
    /// Stops this entity to stop packets with the specified frequency from being jammmed.
    /// </summary>
    public void RemoveExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent, DeviceFrequency frequency)
    {
        if (ent.Comp.FrequenciesExcluded.Remove(frequency))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.FrequenciesExcluded));
    }

    /// <summary>
    /// Stops this entity to stop packets with any frequency from being jammmed.
    /// </summary>
    public void ClearExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent)
    {
        if (ent.Comp.FrequenciesExcluded.Count == 0)
            return;

        ent.Comp.FrequenciesExcluded.Clear();
        DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.FrequenciesExcluded));
    }
}
