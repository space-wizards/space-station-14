using System.Linq;
using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Map.Events;

namespace Content.Server.DeviceNetwork.Systems;

[UsedImplicitly]
public sealed class DeviceListSystem : SharedDeviceListSystem
{
    [Dependency] private readonly NetworkConfiguratorSystem _configurator = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceListComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DeviceListComponent, BeforeBroadcastAttemptEvent>(OnBeforeBroadcast);
        SubscribeLocalEvent<DeviceListComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        SubscribeLocalEvent<BeforeSerializationEvent>(OnMapSave);
    }

    private void OnShutdown(EntityUid uid, DeviceListComponent component, ComponentShutdown args)
    {
        foreach (var conf in component.Configurators)
        {
            _configurator.OnDeviceListShutdown(conf, (uid, component));
        }

        var query = GetEntityQuery<DeviceNetworkComponent>();
        foreach (var device in component.Devices)
        {
            if (query.TryGetComponent(device, out var comp))
                comp.DeviceLists.Remove(uid);
        }
        component.Devices.Clear();
    }

    /// <summary>
    /// Gets the given device list as a dictionary
    /// </summary>
    /// <remarks>
    /// If any entity in the device list is pre-map init, it will show the entity UID of the device instead.
    /// </remarks>
    public Dictionary<string, EntityUid> GetDeviceList(EntityUid uid, DeviceListComponent? deviceList = null)
    {
        if (!Resolve(uid, ref deviceList))
            return new Dictionary<string, EntityUid>();

        var devices = new Dictionary<string, EntityUid>(deviceList.Devices.Count);

        foreach (var deviceUid in deviceList.Devices)
        {
            if (!TryComp(deviceUid, out DeviceNetworkComponent? deviceNet))
                continue;

            var address = MetaData(deviceUid).EntityLifeStage == EntityLifeStage.MapInitialized
                ? deviceNet.Address
                : $"UID: {deviceUid.ToString()}";

            devices.Add(address, deviceUid);

        }

        return devices;
    }

    /// <summary>
    /// Checks if the given address is present in a device list
    /// </summary>
    /// <param name="uid">The entity uid that has the device list that should be checked for the address</param>
    /// <param name="address">The address to check for</param>
    /// <param name="deviceList">The device list component</param>
    /// <returns>True if the address is present. False if not</returns>
    public bool ExistsInDeviceList(EntityUid uid, string address, DeviceListComponent? deviceList = null)
    {
        var addresses = GetDeviceList(uid).Keys;
        return addresses.Contains(address);
    }

    /// <summary>
    /// Filters the broadcasts recipient list against the device list as either an allow or deny list depending on the components IsAllowList field
    /// </summary>
    private void OnBeforeBroadcast(EntityUid uid, DeviceListComponent component, BeforeBroadcastAttemptEvent args)
    {
        //Don't filter anything if the device list is empty
        if (component.Devices.Count == 0)
        {
            if (component.IsAllowList)
                args.Cancel();
            return;
        }

        HashSet<DeviceNetworkComponent> filteredRecipients = new(args.Recipients.Count);

        foreach (var recipient in args.Recipients)
        {
            if (component.Devices.Contains(recipient.Owner) == component.IsAllowList)
                filteredRecipients.Add(recipient);
        }

        args.ModifiedRecipients = filteredRecipients;
    }

    /// <summary>
    /// Filters incoming packets if that is enabled <see cref="OnBeforeBroadcast"/>
    /// </summary>
    private void OnBeforePacketSent(EntityUid uid, DeviceListComponent component, BeforePacketSentEvent args)
    {
        if (component.HandleIncomingPackets && component.Devices.Contains(args.Sender) != component.IsAllowList)
            args.Cancel();
    }

    public void OnDeviceShutdown(Entity<DeviceListComponent?> list, Entity<DeviceNetworkComponent> device)
    {
        device.Comp.DeviceLists.Remove(list.Owner);
        if (!Resolve(list.Owner, ref list.Comp))
            return;

        list.Comp.Devices.Remove(device);
        Dirty(list);
    }

    private void OnMapSave(BeforeSerializationEvent ev)
    {
        List<EntityUid> toRemove = new();
        var query = GetEntityQuery<TransformComponent>();
        var enumerator = AllEntityQuery<DeviceListComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var device, out var xform))
        {
            if (!ev.MapIds.Contains(xform.MapID))
                continue;

            foreach (var ent in device.Devices)
            {
                if (!query.TryGetComponent(ent, out var linkedXform))
                {
                    // Entity was deleted.
                    // TODO remove these on deletion instead of on-save.
                    toRemove.Add(ent);
                    continue;
                }

                // This is assuming that **all** of the map is getting saved.
                // Which is not necessarily true.
                // AAAAAAAAAAAAAA
                if (ev.MapIds.Contains(linkedXform.MapID))
                    continue;

                toRemove.Add(ent);
                // TODO full game saves.
                // when full saves are supported, this should instead add data to the BeforeSaveEvent informing the
                // saving system that this map (or null-space entity) also needs to be included in the save.
                Log.Error(
                    $"Saving a device list ({ToPrettyString(uid)}) that has a reference to an entity on another map ({ToPrettyString(ent)}). Removing entity from list.");
            }

            if (toRemove.Count == 0)
                continue;

            var old = device.Devices.ToList();
            device.Devices.ExceptWith(toRemove);
            RaiseLocalEvent(uid, new DeviceListUpdateEvent(old, device.Devices.ToList()));
            Dirty(uid, device);
            toRemove.Clear();
        }
    }

    /// <summary>
    ///     Updates the device list stored on this entity.
    /// </summary>
    /// <param name="uid">The entity to update.</param>
    /// <param name="devices">The devices to store.</param>
    /// <param name="merge">Whether to merge or replace the devices stored.</param>
    /// <param name="deviceList">Device list component</param>
    public DeviceListUpdateResult UpdateDeviceList(EntityUid uid, IEnumerable<EntityUid> devices, bool merge = false, DeviceListComponent? deviceList = null)
    {
        if (!Resolve(uid, ref deviceList))
            return DeviceListUpdateResult.NoComponent;

        var list = devices.ToList();
        var newDevices = new HashSet<EntityUid>(list);

        if (merge)
            newDevices.UnionWith(deviceList.Devices);

        if (newDevices.Count > deviceList.DeviceLimit)
        {
            return DeviceListUpdateResult.TooManyDevices;
        }

        var query = GetEntityQuery<DeviceNetworkComponent>();
        var oldDevices = deviceList.Devices.ToList();
        foreach (var device in oldDevices)
        {
            if (newDevices.Contains(device))
                continue;

            deviceList.Devices.Remove(device);
            if (query.TryGetComponent(device, out var comp))
                comp.DeviceLists.Remove(uid);
        }

        foreach (var device in newDevices)
        {
            if (!query.TryGetComponent(device, out var comp))
                continue;

            if (!deviceList.Devices.Add(device))
                continue;

            comp.DeviceLists.Add(uid);
        }

        RaiseLocalEvent(uid, new DeviceListUpdateEvent(oldDevices, list));

        Dirty(uid, deviceList);

        return DeviceListUpdateResult.UpdateOk;
    }
}
