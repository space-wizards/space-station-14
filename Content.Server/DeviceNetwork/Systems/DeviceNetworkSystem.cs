using Content.Shared.DeviceNetwork;
using Content.Server.Buffers;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Server.DeviceNetwork.Systems;

/// <inheritdoc/>
public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    [Dependency] private DeviceListSystem _deviceLists = default!;
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;

    public override void Initialize()
    {
        base.Initialize();
        DeviceArrayPool = new ServerRobustArrayPool<Device>();
        EntityArrayPool = new ServerRobustArrayPool<EntityUid?>();
    }

    /// Automatically disconnect when an entity with a DeviceNetworkComponent shuts down.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnNetworkShutdown(Entity<DeviceNetworkComponent> ent, ref ComponentShutdown args)
    {
        var component = ent.Comp;
        foreach (var list in component.DeviceLists)
        {
            if (Deleted(list))
                return;

            _deviceLists.OnDeviceShutdown(list, ent);
        }

        foreach (var list in component.Configurators)
        {
            if (Deleted(list))
                return;

            _configurator.OnDeviceShutdown(list, ent);
        }

        if (TryGetNetwork(component.DeviceNetId, out var network))
            network.Remove(ent);
    }
}
