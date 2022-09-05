using System.Linq;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork;

public abstract class SharedDeviceListSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceListComponent, ComponentGetState>(GetDeviceListState);
        SubscribeLocalEvent<DeviceListComponent, ComponentHandleState>(HandleDeviceListState);
        SubscribeLocalEvent<SharedNetworkConfiguratorComponent, ComponentGetState>(GetNetworkConfiguratorState);
        SubscribeLocalEvent<SharedNetworkConfiguratorComponent, ComponentHandleState>(HandleNetworkConfiguratorState);
    }

    public void UpdateDeviceList(EntityUid uid, IEnumerable<EntityUid> devices, bool merge = false, DeviceListComponent? deviceList = null)
    {
        if (!Resolve(uid, ref deviceList))
            return;

        if (!merge)
            deviceList.Devices.Clear();

        var devicesList = devices.ToList();
        deviceList.Devices.UnionWith(devicesList);

        RaiseLocalEvent(uid, new DeviceListUpdateEvent(devicesList));

        Dirty(deviceList);
    }

    private void GetDeviceListState(EntityUid uid, DeviceListComponent comp, ComponentGetState args)
    {
        args.State = new DeviceListComponentState(comp.Devices, comp.IsAllowList, comp.HandleIncomingPackets);
    }

    private void HandleDeviceListState(EntityUid uid, DeviceListComponent comp, ComponentHandleState args)
    {
        if (args.Current is not DeviceListComponentState state)
        {
            return;
        }

        comp.Devices = state.Devices;
        comp.HandleIncomingPackets = state.HandleIncomingPackets;
        comp.IsAllowList = state.IsAllowList;
    }

    private void GetNetworkConfiguratorState(EntityUid uid, SharedNetworkConfiguratorComponent comp,
        ComponentGetState args)
    {
        args.State = new NetworkConfiguratorComponentState(comp.ActiveDeviceList);
    }

    private void HandleNetworkConfiguratorState(EntityUid uid, SharedNetworkConfiguratorComponent comp,
        ComponentHandleState args)
    {
        if (args.Current is not NetworkConfiguratorComponentState state)
        {
            return;
        }

        comp.ActiveDeviceList = state.ActiveDeviceList;
    }
}



public sealed class DeviceListUpdateEvent : EntityEventArgs
{
    public DeviceListUpdateEvent(List<EntityUid> devices)
    {
        Devices = devices;
    }

    public List<EntityUid> Devices { get; }
}
