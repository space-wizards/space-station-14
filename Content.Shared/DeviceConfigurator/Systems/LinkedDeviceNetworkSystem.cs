using Content.Shared.DeviceConfigurator.Components;

namespace Content.Shared.DeviceConfigurator.Systems;

public sealed partial class LinkedDeviceNetworkSystem : EntitySystem
{
    [Dependency] private DeviceListSystem _deviceList = default!;
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;

    [SubscribeLocalEvent]
    private void OnNetworkShutdown(Entity<LinkedDeviceNetworkComponent> ent, ref ComponentShutdown args)
    {
        var component = ent.Comp;
        foreach (var list in component.DeviceLists)
        {
            if (Deleted(list))
                return;

            _deviceList.OnDeviceShutdown(list, ent);
        }

        foreach (var list in component.Configurators)
        {
            if (Deleted(list))
                return;

            _configurator.OnDeviceShutdown(list, ent);
        }
    }
}
