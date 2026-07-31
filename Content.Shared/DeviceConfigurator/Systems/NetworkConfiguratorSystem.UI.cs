using Content.Shared.Database;
using Content.Shared.DeviceConfigurator.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Shared.DeviceConfigurator.Systems;

public sealed partial class NetworkConfiguratorSystem
{
    private void OpenDeviceLinkUi(Entity<NetworkConfiguratorComponent> configurator,
        EntityUid? targetUid,
        EntityUid userUid)
    {
        if (_useDelay.IsDelayed(configurator.Owner))
            return;

        if (!targetUid.HasValue || !configurator.Comp.ActiveDeviceLink.HasValue ||
            !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        _uiSystem.OpenUi(configurator.Owner, NetworkConfiguratorUiKey.Link, userUid);
        configurator.Comp.DeviceLinkTarget = targetUid;
        DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.DeviceLinkTarget));

        UpdateLinkUiState(configurator);
    }

    private void UpdateLinkUiState(Entity<NetworkConfiguratorComponent> configurator)
    {
        if (_uiSystem.TryGetOpenUi(configurator.Owner, NetworkConfiguratorUiKey.Link, out var bui))
            bui.Update();
    }

    /// <summary>
    /// Opens the config ui. It can be used to modify the devices in the targets device list.
    /// </summary>
    private void OpenDeviceListUi(Entity<NetworkConfiguratorComponent> configurator,
        EntityUid? targetUid,
        EntityUid userUid)
    {
        if (configurator.Comp.ActiveDeviceLink == targetUid)
            return;

        if (_useDelay.IsDelayed(configurator.Owner))
            return;

        if (!targetUid.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        if (!_deviceListQuery.TryComp(targetUid, out var list))
            return;

        if (_deviceListQuery.TryComp(configurator.Comp.ActiveDeviceList, out var oldList))
        {
            oldList.Configurators.Remove(configurator);
            DirtyField(configurator.Comp.ActiveDeviceList.Value, oldList, nameof(DeviceListComponent.Configurators));
        }

        list.Configurators.Add(configurator);
        configurator.Comp.ActiveDeviceList = targetUid;
        DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceList));
        DirtyField(targetUid.Value, list, nameof(DeviceListComponent.Configurators));

        if (_uiSystem.TryOpenUi(configurator.Owner, NetworkConfiguratorUiKey.Configure, userUid))
        {
            if (_uiSystem.TryGetOpenUi(configurator.Owner, NetworkConfiguratorUiKey.Configure, out var bui))
                bui.Update();

            /*_uiSystem.SetUiState(configurator.Owner,
                NetworkConfiguratorUiKey.Configure,
                new DeviceListUserInterfaceState(
                    _deviceListSystem.GetDeviceList(configurator.Comp.ActiveDeviceList.Value)
                        .Select(v => (v.Key, MetaData(v.Value.Item1).EntityName))
                        .ToHashSet()
                ));*/
        }
    }

    /// <summary>
    /// Updates the list of <see cref="NetworkConfiguratorComponent.NamedDevices"/>
    /// and the NetworkConfigurator's List UI menu if it's opened.
    /// </summary>
    private void UpdateListUiState(Entity<NetworkConfiguratorComponent> ent)
    {
        ClearInvalidDevices(ent);

        if (_uiSystem.TryGetOpenUi(ent.Owner, NetworkConfiguratorUiKey.List, out var bui))
            bui.Update();
    }

    /// <summary>
    /// Clears the active device list when the ui is closed
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUiClosed(Entity<NetworkConfiguratorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(NetworkConfiguratorUiKey.Configure)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.Link)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.List))
        {
            return;
        }

        if (_deviceListQuery.TryComp(ent.Comp.ActiveDeviceList, out var list))
        {
            list.Configurators.Remove(ent);
            DirtyField(ent.Comp.ActiveDeviceList.Value, list, nameof(DeviceListComponent.Configurators));
        }

        ent.Comp.ActiveDeviceList = null;

        if (args.UiKey is NetworkConfiguratorUiKey.Link)
        {
            ent.Comp.ActiveDeviceLink = null;
            ent.Comp.DeviceLinkTarget = null;
            DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceLink));
            DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.DeviceLinkTarget));
        }

        DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceList));
    }

    public void OnDeviceListShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<DeviceListComponent> list)
    {
        list.Comp.Configurators.Remove(conf.Owner);
        if (_networkConfigQuery.Resolve(conf.Owner, ref conf.Comp))
            conf.Comp.ActiveDeviceList = null;

        DirtyField(list.AsNullable(), nameof(DeviceListComponent.Configurators));
        DirtyField(conf, nameof(NetworkConfiguratorComponent.ActiveDeviceList));
    }

    /// <summary>
    /// Removes a device from the saved devices list
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRemoveDevice(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorRemoveDeviceMessage args)
    {
        if (ent.Comp.Devices.TryGetValue(args.Address.AddressId, out var removedDevice))
        {
            _adminLogger.Add(LogType.DeviceLinking,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):actor} removed buffered device {ToPrettyString(removedDevice):subject} from {ToPrettyString(ent):tool}");
        }

        ent.Comp.Devices.Remove(args.Address.AddressId);
        ent.Comp.NamedDevices.Remove(args.Address.AddressId);
        if (_linkedDeviceQuery.TryComp(removedDevice, out var device))
        {
            device.Configurators.Remove(ent);
            DirtyField(removedDevice, device, nameof(LinkedDeviceNetworkComponent.Configurators));
        }

        UpdateListUiState(ent);
        DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.Devices));
    }

    [SubscribeLocalEvent]
    private void OnClearDevice(Entity<NetworkConfiguratorComponent> ent,
        ref NetworkConfiguratorListClearDevicesMessage args)
    {
        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared buffered devices from {ToPrettyString(ent):tool}");

        ClearDevices(ent);
        UpdateListUiState(ent);
    }

    private void ClearDevices(Entity<NetworkConfiguratorComponent> ent)
    {
        foreach (var device in ent.Comp.Devices.Values)
        {
            if (!_linkedDeviceQuery.TryComp(device, out var comp))
                continue;

            comp.Configurators.Remove(ent);
            DirtyField(device, comp, nameof(LinkedDeviceNetworkComponent.Configurators));
        }

        ent.Comp.Devices.Clear();
        ent.Comp.NamedDevices.Clear();
        DirtyFields(ent.AsNullable(), null, nameof(NetworkConfiguratorComponent.Devices), nameof(NetworkConfiguratorComponent.NamedDevices));
    }

    [SubscribeLocalEvent]
    private void OnClearLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorLinkClearMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared links between {ToPrettyString(ent.Comp.ActiveDeviceLink.Value):subject} and {ToPrettyString(ent.Comp.DeviceLinkTarget.Value):subject2} with {ToPrettyString(ent):tool}");

        if (_deviceLinkSourceQuery.HasComp(ent.Comp.ActiveDeviceLink)
            && _deviceLinkSinkQuery.HasComp(ent.Comp.DeviceLinkTarget))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                ent.Comp.ActiveDeviceLink.Value,
                ent.Comp.DeviceLinkTarget.Value);

            UpdateLinkUiState(ent);
        }
        else if (_deviceLinkSourceQuery.HasComp(ent.Comp.DeviceLinkTarget)
                 && _deviceLinkSinkQuery.HasComp(ent.Comp.ActiveDeviceLink))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                ent.Comp.DeviceLinkTarget.Value,
                ent.Comp.ActiveDeviceLink.Value);

            UpdateLinkUiState(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnToggleLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorLinkToggleMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        if (_deviceLinkSourceQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSource)
            && _deviceLinkSinkQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                (ent.Comp.DeviceLinkTarget.Value, targetSink),
                args.Link);

            UpdateLinkUiState(ent);
        }
        else if (_deviceLinkSourceQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSource)
                 && _deviceLinkSinkQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                (ent.Comp.ActiveDeviceLink.Value, activeSink),
                args.Link);

            UpdateLinkUiState(ent);
        }
    }

    /// <summary>
    /// Saves links set by the device link UI
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSaveLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorLinkSaveMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        if (_deviceLinkSourceQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSource)
            && _deviceLinkSinkQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                (ent.Comp.DeviceLinkTarget.Value, targetSink),
                args.Links);

            UpdateLinkUiState(ent);
        }
        else if (_deviceLinkSourceQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSource)
                 && _deviceLinkSinkQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                (ent.Comp.ActiveDeviceLink.Value, activeSink),
                args.Links);

            UpdateLinkUiState(ent);
        }
    }

    private void AfterButtonPressed(Entity<NetworkConfiguratorComponent> ent, EntityUid actor, DeviceListUpdateResult result)
    {
        if (ent.Comp.ActiveDeviceList == null)
            return;

        var resultText = result switch
        {
            DeviceListUpdateResult.TooManyDevices => Loc.GetString("network-configurator-too-many-devices"),
            DeviceListUpdateResult.UpdateOk => Loc.GetString("network-configurator-update-ok"),
            _ => "error"
        };

        _popupSystem.PopupCursor(Loc.GetString(resultText), actor, PopupType.Medium);

        if (_uiSystem.TryGetOpenUi(ent.Owner, NetworkConfiguratorUiKey.Configure, out var bui))
            bui.Update();
    }

    [SubscribeLocalEvent]
    private void OnConfigButtonPressed(Entity<NetworkConfiguratorComponent> ent,ref NetworkConfiguratorSetMessage args)
    {
        if (!ent.Comp.ActiveDeviceList.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} set device links to {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} with {ToPrettyString(ent):tool}");

        var result = _deviceListSystem.UpdateDeviceList(ent.Comp.ActiveDeviceList.Value,
            new HashSet<EntityUid>(ent.Comp.Devices.Values));

        AfterButtonPressed(ent, args.Actor, result);
    }

    [SubscribeLocalEvent]
    private void OnConfigButtonPressed(Entity<NetworkConfiguratorComponent> ent,ref NetworkConfiguratorAddMessage args)
    {
        if (!ent.Comp.ActiveDeviceList.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} added device links to {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} with {ToPrettyString(ent):tool}");

        var result = _deviceListSystem.UpdateDeviceList(ent.Comp.ActiveDeviceList.Value,
            new HashSet<EntityUid>(ent.Comp.Devices.Values),
            true);

        AfterButtonPressed(ent, args.Actor, result);
    }

    [SubscribeLocalEvent]
    private void OnConfigButtonPressed(Entity<NetworkConfiguratorComponent> ent,ref NetworkConfiguratorCopyMessage args)
    {
        if (!ent.Comp.ActiveDeviceList.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} copied devices from {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} to {ToPrettyString(ent):tool}");

        ClearDevices(ent);

        foreach (var (addr, device) in _deviceListSystem.GetDeviceList(ent.Comp.ActiveDeviceList.Value))
        {
            if (!_linkedDeviceQuery.TryComp(device.Item1, out var comp)
                || !_deviceNetworkQuery.TryComp(device.Item1, out var deviceComp))
                continue;

            var name = Identity.Name(device.Item1, EntityManager, ent);

            ent.Comp.Devices.Add(addr.AddressId, device.Item1);
            ent.Comp.NamedDevices.Add(addr.AddressId, (deviceComp.Prefix, name));

            comp.Configurators.Add(ent);
            DirtyField(device.Item1, comp, nameof(LinkedDeviceNetworkComponent.Configurators));
        }

        DirtyFields(ent.AsNullable(), null, nameof(NetworkConfiguratorComponent.Devices), nameof(NetworkConfiguratorComponent.NamedDevices));
        AfterButtonPressed(ent, args.Actor, DeviceListUpdateResult.UpdateOk);
        UpdateListUiState(ent);
    }

    [SubscribeLocalEvent]
    private void OnConfigButtonPressed(Entity<NetworkConfiguratorComponent> ent,ref NetworkConfiguratorClearMessage args)
    {
        if (!ent.Comp.ActiveDeviceList.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared device links from {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} with {ToPrettyString(ent):tool}");

        var result = _deviceListSystem.UpdateDeviceList(ent.Comp.ActiveDeviceList.Value, new HashSet<EntityUid>());

        AfterButtonPressed(ent, args.Actor, result);
    }

    public void OnDeviceShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<LinkedDeviceNetworkComponent> device)
    {
        device.Comp.Configurators.Remove(conf.Owner);
        DirtyField(device.AsNullable(), nameof(LinkedDeviceNetworkComponent.Configurators));

        if (!Resolve(conf.Owner, ref conf.Comp))
            return;

        foreach (var (addr, dev) in conf.Comp.Devices)
        {
            if (device.Owner != dev)
                continue;

            conf.Comp.Devices.Remove(addr);
            conf.Comp.NamedDevices.Remove(addr);
            DirtyFields(conf, null, nameof(NetworkConfiguratorComponent.Devices), nameof(NetworkConfiguratorComponent.NamedDevices));
        }

        UpdateListUiState(conf!);
    }
}
