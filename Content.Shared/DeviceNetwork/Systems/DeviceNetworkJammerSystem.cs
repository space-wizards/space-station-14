using Content.Shared.DeviceNetwork.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.DeviceNetwork.Systems;

/// <inheritdoc/>
public sealed partial class DeviceNetworkJammerSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _device = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<Entity<DeviceNetworkComponent>> _cache = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DeviceNetworkJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var jammerComp, out var xform))
        {
            if (jammerComp.NextUpdate > _timing.CurTime)
                continue;

            jammerComp.NextUpdate = _timing.CurTime + jammerComp.Delay;

            _cache.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, jammerComp.Range, _cache);

            var jamNetworks = GetJammableNetworks((uid, jammerComp));

            foreach (var device in _cache)
            {
                if (device.Comp.TransmitFrequency != null
                    && jammerComp.FrequenciesExcluded.Contains(device.Comp.TransmitFrequency.Value)
                    || device.Comp.ReceiveFrequency != null
                    && jammerComp.FrequenciesExcluded.Contains(device.Comp.ReceiveFrequency.Value))
                    continue;

                _device.JamDevice(device.AsNullable(), jammerComp.Delay, jamNetworks);
            }
        }
    }

    /// <summary>
    /// Sets the range of the jamming effect.
    /// </summary>
    [PublicAPI]
    public void SetRange(Entity<DeviceNetworkJammerComponent> ent, float value)
    {
        ent.Comp.Range = value;
        DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.Range));
    }

    /// <inheritdoc cref="SetRange"/>
    [PublicAPI]
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
    [PublicAPI]
    public IReadOnlySet<ProtoId<DeviceNetworkPrototype>> GetJammableNetworks(Entity<DeviceNetworkJammerComponent> ent)
    {
        return ent.Comp.JammableNetworks;
    }

    /// <summary>
    /// Enables this entity to jam packets on the specified network.
    /// </summary>
    [PublicAPI]
    public void AddJammableNetwork(Entity<DeviceNetworkJammerComponent> ent, ProtoId<DeviceNetworkPrototype> networkId)
    {
        if (ent.Comp.JammableNetworks.Add(networkId))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.JammableNetworks));
    }

    [PublicAPI]
    public void AddJammableNetworks(Entity<DeviceNetworkJammerComponent> ent, HashSet<ProtoId<DeviceNetworkPrototype>> networks)
    {
        ent.Comp.JammableNetworks.UnionWith(networks);
        DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.JammableNetworks));
    }

    /// <summary>
    /// Stops this entity from jamming packets on the specified network.
    /// </summary>
    [PublicAPI]
    public void RemoveJammableNetwork(Entity<DeviceNetworkJammerComponent> ent, ProtoId<DeviceNetworkPrototype> networkId)
    {
        if (ent.Comp.JammableNetworks.Remove(networkId))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.JammableNetworks));
    }

    /// <summary>
    /// Stops this entity from jamming packets on any networks.
    /// </summary>
    [PublicAPI]
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
    [PublicAPI]
    public void AddExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent, DeviceFrequency frequency)
    {
        if (ent.Comp.FrequenciesExcluded.Add(frequency))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.FrequenciesExcluded));
    }

    /// <summary>
    /// Stops this entity to stop packets with the specified frequency from being jammmed.
    /// </summary>
    [PublicAPI]
    public void RemoveExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent, DeviceFrequency frequency)
    {
        if (ent.Comp.FrequenciesExcluded.Remove(frequency))
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.FrequenciesExcluded));
    }

    /// <summary>
    /// Stops this entity to stop packets with any frequency from being jammmed.
    /// </summary>
    [PublicAPI]
    public void ClearExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent)
    {
        if (ent.Comp.FrequenciesExcluded.Count == 0)
            return;

        ent.Comp.FrequenciesExcluded.Clear();
        DirtyField(ent.AsNullable(), nameof(DeviceNetworkJammerComponent.FrequenciesExcluded));
    }
}
