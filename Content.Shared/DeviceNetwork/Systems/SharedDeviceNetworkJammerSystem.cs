using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Systems;

/// <inheritdoc cref="DeviceNetworkJammerComponent"/>
public abstract class SharedDeviceNetworkJammerSystem : EntitySystem
{
    /// <summary>
    /// Sets the range of the jamming effect.
    /// </summary>
    public void SetRange(Entity<DeviceNetworkJammerComponent> ent, float value)
    {
        ent.Comp.Range = value;
        Dirty(ent);
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
    public IReadOnlySet<string> GetJammableNetworks(Entity<DeviceNetworkJammerComponent> ent)
    {
        return ent.Comp.JammableNetworks;
    }

    /// <summary>
    /// Enables this entity to jam packets on the specified network.
    /// </summary>
    public void AddJammableNetwork(Entity<DeviceNetworkJammerComponent> ent, string networkId)
    {
        if (ent.Comp.JammableNetworks.Add(networkId))
            Dirty(ent);
    }

    /// <summary>
    /// Stops this entity from jamming packets on the specified network.
    /// </summary>
    public void RemoveJammableNetwork(Entity<DeviceNetworkJammerComponent> ent, string networkId)
    {
        if (ent.Comp.JammableNetworks.Remove(networkId))
            Dirty(ent);
    }

    /// <summary>
    /// Stops this entity from jamming packets on any networks.
    /// </summary>
    public void ClearJammableNetworks(Entity<DeviceNetworkJammerComponent> ent)
    {
        if (ent.Comp.JammableNetworks.Count == 0)
            return;

        ent.Comp.JammableNetworks.Clear();
        Dirty(ent);
    }

    /// <summary>
    /// Enables this entity to stop packets with the specified frequency from being jammmed.
    /// </summary>
    public void AddExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent, uint frequency)
    {
        if (ent.Comp.FrequenciesExcluded.Add(frequency))
            Dirty(ent);
    }

    /// <summary>
    /// Stops this entity to stop packets with the specified frequency from being jammmed.
    /// </summary>
    public void RemoveExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent, uint frequency)
    {
        if (ent.Comp.FrequenciesExcluded.Remove(frequency))
            Dirty(ent);
    }

    /// <summary>
    /// Stops this entity to stop packets with any frequency from being jammmed.
    /// </summary>
    public void ClearExcludedFrequency(Entity<DeviceNetworkJammerComponent> ent)
    {
        if (ent.Comp.FrequenciesExcluded.Count == 0)
            return;

        ent.Comp.FrequenciesExcluded.Clear();
        Dirty(ent);
    }
}
