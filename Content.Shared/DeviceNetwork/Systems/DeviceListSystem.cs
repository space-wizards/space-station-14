using System.Linq;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.Map.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceListSystem : EntitySystem
{
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;
    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceListComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DeviceListComponent, BeforeBroadcastAttemptEvent>(OnBeforeBroadcast);
        SubscribeLocalEvent<DeviceListComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        SubscribeLocalEvent<BeforeSerializationEvent>(OnMapSave);
    }

    private void OnShutdown(Entity<DeviceListComponent> ent, ref ComponentShutdown args)
    {
        foreach (var conf in ent.Comp.Configurators)
        {
            _configurator.OnDeviceListShutdown(conf, ent);
        }

        foreach (var device in ent.Comp.Devices)
        {
            if (_deviceNetworkQuery.TryGetComponent(device, out var comp))
                comp.DeviceLists.Remove(ent);
        }

        ent.Comp.Devices.Clear();
    }

    public IEnumerable<EntityUid> GetAllDevices(Entity<DeviceListComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
        {
            return new EntityUid[] { };
        }

        return ent.Comp.Devices;
    }

    /// <summary>
    /// Gets the given device list as a dictionary
    /// </summary>
    /// <remarks>
    /// If any entity in the device list is pre-map init, it will show the entity UID of the device instead.
    /// </remarks>
    public Dictionary<string, EntityUid> GetDeviceList(Entity<DeviceListComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return new Dictionary<string, EntityUid>();

        var devices = new Dictionary<string, EntityUid>(ent.Comp.Devices.Count);

        foreach (var deviceUid in ent.Comp.Devices)
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
    /// <param name="ent">The entity that has the device list that should be checked for the address</param>
    /// <param name="address">The address to check for</param>
    /// <returns>True if the address is present. False if not</returns>
    public bool ExistsInDeviceList(Entity<DeviceListComponent?> ent, string address)
    {
        var addresses = GetDeviceList(ent).Keys;
        return addresses.Contains(address);
    }

    /// <summary>
    /// Filters the broadcasts recipient list against the device list as either an allow or deny list depending on the components IsAllowList field
    /// </summary>
    private void OnBeforeBroadcast(Entity<DeviceListComponent> ent, ref BeforeBroadcastAttemptEvent args)
    {
        var component = ent.Comp;
        //Don't filter anything if the device list is empty
        if (component.Devices.Count == 0)
        {
            if (component.IsAllowList)
                args.Cancelled = true;
            return;
        }

        HashSet<Device> filteredRecipients = new(args.Recipients.Count);

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
    private void OnBeforePacketSent(Entity<DeviceListComponent> ent, ref BeforePacketSentEvent args)
    {
        if (ent.Comp.HandleIncomingPackets && ent.Comp.Devices.Contains(args.Sender) != ent.Comp.IsAllowList)
            args.Cancelled = true;
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
        var enumerator = AllEntityQuery<DeviceListComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var device, out var xform))
        {
            if (!ev.MapIds.Contains(xform.MapID))
                continue;

            foreach (var ent in device.Devices)
            {
                if (!TryComp(ent, out TransformComponent? linkedXform))
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
    /// <param name="ent">The entity to update.</param>
    /// <param name="devices">The devices to store.</param>
    /// <param name="merge">Whether to merge or replace the devices stored.</param>
    public DeviceListUpdateResult UpdateDeviceList(Entity<DeviceListComponent?> ent, IEnumerable<EntityUid> devices, bool merge = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return DeviceListUpdateResult.NoComponent;

        var list = devices.ToList();
        var newDevices = new HashSet<EntityUid>(list);

        if (merge)
            newDevices.UnionWith(ent.Comp.Devices);

        if (newDevices.Count > ent.Comp.DeviceLimit)
        {
            return DeviceListUpdateResult.TooManyDevices;
        }

        var oldDevices = ent.Comp.Devices.ToList();
        foreach (var device in oldDevices)
        {
            if (newDevices.Contains(device))
                continue;

            ent.Comp.Devices.Remove(device);
            if (_deviceNetworkQuery.TryGetComponent(device, out var comp))
                comp.DeviceLists.Remove(ent);
        }

        foreach (var device in newDevices)
        {
            if (!_deviceNetworkQuery.TryGetComponent(device, out var comp))
                continue;

            if (!ent.Comp.Devices.Add(device))
                continue;

            comp.DeviceLists.Add(ent);
        }

        RaiseLocalEvent(ent, new DeviceListUpdateEvent(oldDevices, list));

        Dirty(ent);

        return DeviceListUpdateResult.UpdateOk;
    }
}
