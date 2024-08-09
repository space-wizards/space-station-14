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
}
