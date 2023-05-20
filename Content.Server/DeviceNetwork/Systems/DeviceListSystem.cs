using System.Linq;
using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems;

[UsedImplicitly]
public sealed class DeviceListSystem : SharedDeviceListSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceListComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DeviceListComponent, BeforeBroadcastAttemptEvent>(OnBeforeBroadcast);
        SubscribeLocalEvent<DeviceListComponent, BeforePacketSentEvent>(OnBeforePacketSent);
    }

    public void OnInit(EntityUid uid, DeviceListComponent component, ComponentInit args)
    {
        Dirty(component);
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
    /// Filters the broadcasts recipient list against the device list as either an allow or deny list depending on the components IsAllowList field
    /// </summary>
    private void OnBeforeBroadcast(EntityUid uid, DeviceListComponent component, BeforeBroadcastAttemptEvent args)
    {
        //Don't filter anything if the device list is empty
        if (component.Devices.Count == 0)
        {
            if (component.IsAllowList) args.Cancel();
            return;
        }

        HashSet<DeviceNetworkComponent> filteredRecipients = new(args.Recipients.Count);

        foreach (var recipient in args.Recipients)
        {
            if (component.Devices.Contains(recipient.Owner) == component.IsAllowList) filteredRecipients.Add(recipient);
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
}
