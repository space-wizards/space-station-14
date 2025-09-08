using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.DeviceNetwork.Systems;

[UsedImplicitly]
public sealed class NetworkConfiguratorSystem : SharedNetworkConfiguratorSystem
{
    [Dependency] private readonly DeviceListSystem _deviceListSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetworkConfiguratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NetworkConfiguratorComponent, ComponentShutdown>(OnShutdown);

        //UI
        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorRemoveDeviceMessage>(OnRemoveDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearDevicesMessage>(OnClearDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorLinksSaveMessage>(OnSaveLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearLinksMessage>(OnClearLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorToggleLinkMessage>(OnToggleLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorButtonPressedMessage>(OnConfigButtonPressed);

        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUserInterfaceCheckRangeEvent>(OnUiRangeCheck);

        SubscribeLocalEvent<DeviceListComponent, ComponentRemove>(OnComponentRemoved);

        SubscribeLocalEvent<BeforeSerializationEvent>(OnMapSave);
    }

    private void OnMapSave(BeforeSerializationEvent ev)
    {
        var enumerator = AllEntityQuery<NetworkConfiguratorComponent>();
        while (enumerator.MoveNext(out var uid, out var conf))
        {
            if (!TryComp(conf.ActiveDeviceList, out TransformComponent? listXform))
                continue;

            if (!ev.MapIds.Contains(listXform.MapID))
                continue;

            // The linked device list is (probably) being saved. Make sure that the configurator is also being saved
            // (i.e., not in the hands of a mapper/ghost). In the future, map saving should raise a separate event
            // containing a set of all entities that are about to be saved, which would make checking this much easier.
            // This is a shitty bandaid, and will force close the UI during auto-saves.
            // TODO Map serialization refactor
            // I'm refactoring it now and I still dont know what to do

            var xform = Transform(uid);
            if (ev.MapIds.Contains(xform.MapID) && IsSaveable(uid))
                continue;

            _uiSystem.CloseUi(uid, NetworkConfiguratorUiKey.Configure);
            DebugTools.AssertNull(conf.ActiveDeviceList);
        }

        bool IsSaveable(EntityUid uid)
        {
            while (uid.IsValid())
            {
                if (Prototype(uid)?.MapSavable == false)
                    return false;
                uid = Transform(uid).ParentUid;
            }
            return true;
        }
    }

    private void OnUiRangeCheck(Entity<NetworkConfiguratorComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (ent.Comp.ActiveDeviceList == null || args.Result == BoundUserInterfaceRangeResult.Fail)
            return;

        DebugTools.Assert(Exists(ent.Comp.ActiveDeviceList));
        if (!_interactionSystem.InRangeUnobstructed(args.Actor!, ent.Comp.ActiveDeviceList.Value))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void OnShutdown(Entity<NetworkConfiguratorComponent> ent, ref ComponentShutdown args)
    {
        ClearDevices(ent, ent);

        if (TryComp(ent, out DeviceListComponent? list))
            list.Configurators.Remove(ent.Owner);
        ent.Comp.ActiveDeviceList = null;
    }

    private void OnMapInit(Entity<NetworkConfiguratorComponent> ent, ref MapInitEvent args)
    {
        UpdateListUiState(ent, ent);
    }

    protected override void TryAddNetworkDevice(
        EntityUid configuratorUid,
        EntityUid? targetUid,
        EntityUid userUid,
        NetworkConfiguratorComponent? configurator = null,
        DeviceNetworkComponent? device = null)
    {
        if (!targetUid.HasValue
            || !Resolve(configuratorUid, ref configurator)
            || !Resolve(targetUid.Value, ref device, false))
            return;

        //This checks if the device is marked as having a savable address,
        //to avoid adding pdas and whatnot to air alarms. This flag is true
        //by default, so this will only prevent devices from being added to
        //network configurator lists if manually set to false in the prototype
        if (!device.SavableAddress)
            return;

        var address = device.Address;
        if (string.IsNullOrEmpty(address))
        {
            // This primarily checks if the entity in question is pre-map init or not.
            // This is because otherwise, anything that uses DeviceNetwork will not
            // have an address populated, as all devices that use DeviceNetwork
            // obtain their address on map init. If the entity is post-map init,
            // and it still doesn't have an address, it will fail. Otherwise,
            // it stores the entity's UID as a string for visual effect, that way
            // a mapper can reference the devices they've gathered by UID, instead of
            // by device network address. These entries, if the multitool is still in
            // the map after it being saved, are cleared upon mapinit.
            if (MetaData(targetUid.Value).EntityLifeStage == EntityLifeStage.MapInitialized)
            {
                _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-failed", ("device", targetUid)),
                    userUid);
                return;
            }

            address = $"UID: {targetUid.Value}";
        }

        if (configurator.Devices.ContainsValue(targetUid.Value))
        {
            _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-already-saved", ("device", targetUid)), userUid);
            return;
        }

        device.Configurators.Add(configuratorUid);
        configurator.Devices.Add(address, targetUid.Value);
        _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-saved", ("address", device.Address), ("device", targetUid)),
            userUid, PopupType.Medium);

        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userUid):actor} saved {ToPrettyString(targetUid.Value):subject} to {ToPrettyString(configuratorUid):tool}");

        UpdateListUiState(configuratorUid, configurator);
    }

    protected override void TryLinkDevice(EntityUid uid, NetworkConfiguratorComponent configurator, EntityUid? target, EntityUid user)
    {
        if (!HasComp<DeviceLinkSourceComponent>(target) && !HasComp<DeviceLinkSinkComponent>(target))
            return;

        if (configurator.ActiveDeviceLink == target)
        {
            _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-stopped"), target.Value, user);
            configurator.ActiveDeviceLink = null;
            Dirty(uid, configurator);
            return;
        }

        if (configurator.ActiveDeviceLink.HasValue
            && (HasComp<DeviceLinkSourceComponent>(target)
            && HasComp<DeviceLinkSinkComponent>(configurator.ActiveDeviceLink)
            || HasComp<DeviceLinkSinkComponent>(target)
            && HasComp<DeviceLinkSourceComponent>(configurator.ActiveDeviceLink)))
        {
            OpenDeviceLinkUi(uid, target, user, configurator);
            return;
        }

        if (HasComp<DeviceLinkSourceComponent>(target) && HasComp<DeviceLinkSourceComponent>(configurator.ActiveDeviceLink)
            || HasComp<DeviceLinkSinkComponent>(target) && HasComp<DeviceLinkSinkComponent>(configurator.ActiveDeviceLink))
            return;

        _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-started", ("device", Name(target.Value))), target.Value, user);
        configurator.ActiveDeviceLink = target;
        Dirty(uid, configurator);
    }

    protected override void TryLinkDefaults(
        EntityUid configuratorUid,
        NetworkConfiguratorComponent configurator,
        EntityUid? targetUid,
        EntityUid user)
    {
        if (!configurator.LinkModeActive || !configurator.ActiveDeviceLink.HasValue
            || !targetUid.HasValue || configurator.ActiveDeviceLink == targetUid)
            return;

        if (!HasComp<DeviceLinkSourceComponent>(targetUid) && !HasComp<DeviceLinkSinkComponent>(targetUid))
            return;

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(targetUid, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.LinkDefaults(user, configurator.ActiveDeviceLink.Value, targetUid.Value, activeSource, targetSink);
        }
        else if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink) && TryComp(targetUid, out DeviceLinkSourceComponent? targetSource))
        {
            _deviceLinkSystem.LinkDefaults(user, targetUid.Value, configurator.ActiveDeviceLink.Value, targetSource, activeSink);
        }
    }

    private bool AccessCheck(EntityUid target, EntityUid? user, NetworkConfiguratorComponent component)
    {
        if (!TryComp(target, out AccessReaderComponent? reader) || user == null)
            return true;

        if (_accessSystem.IsAllowed(user.Value, target, reader))
            return true;

        _audioSystem.PlayPvs(component.SoundNoAccess, user.Value, AudioParams.Default.WithVolume(-2f).WithPitchScale(1.2f));
        _popupSystem.PopupEntity(Loc.GetString("network-configurator-device-access-denied"), target, user.Value);

        return false;
    }

    private void OnComponentRemoved(EntityUid uid, DeviceListComponent component, ComponentRemove args)
    {
        _uiSystem.CloseUi(uid, NetworkConfiguratorUiKey.Configure);
    }

    #region UI

    protected override void OpenDeviceLinkUi(
        EntityUid configuratorUid,
        EntityUid? targetUid,
        EntityUid userUid,
        NetworkConfiguratorComponent configurator)
    {
        if (Delay((configuratorUid, configurator)))
            return;

        if (!targetUid.HasValue || !configurator.ActiveDeviceLink.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;


        _uiSystem.OpenUi(configuratorUid, NetworkConfiguratorUiKey.Link, userUid);
        configurator.DeviceLinkTarget = targetUid;
        Dirty(configuratorUid, configurator);

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(targetUid, out DeviceLinkSinkComponent? targetSink))
        {
            UpdateLinkUiState(configuratorUid, configurator.ActiveDeviceLink.Value, targetUid.Value, activeSource, targetSink);
        }
        else if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink)
                 && TryComp(targetUid, out DeviceLinkSourceComponent? targetSource))
        {
            UpdateLinkUiState(configuratorUid, targetUid.Value, configurator.ActiveDeviceLink.Value, targetSource, activeSink);
        }
    }

    private void UpdateLinkUiState(EntityUid configuratorUid, EntityUid sourceUid, EntityUid sinkUid,
        DeviceLinkSourceComponent? sourceComponent = null, DeviceLinkSinkComponent? sinkComponent = null,
        DeviceNetworkComponent? sourceNetworkComponent = null, DeviceNetworkComponent? sinkNetworkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent, false) || !Resolve(sinkUid, ref sinkComponent, false))
            return;

        var sources = _deviceLinkSystem.GetSourcePorts(sourceUid, sourceComponent);
        var sinks = _deviceLinkSystem.GetSinkPortIds((sinkUid, sinkComponent));
        var links = _deviceLinkSystem.GetLinks(sourceUid, sinkUid, sourceComponent);
        var defaults = _deviceLinkSystem.GetDefaults(sources);
        var sourceIds = sources.Select(s => (ProtoId<SourcePortPrototype>)s.ID).ToArray();

        var sourceAddress = Resolve(sourceUid, ref sourceNetworkComponent, false) ? sourceNetworkComponent.Address : "";
        var sinkAddress = Resolve(sinkUid, ref sinkNetworkComponent, false) ? sinkNetworkComponent.Address : "";

        var state = new DeviceLinkUserInterfaceState(sourceIds, sinks, links, sourceAddress, sinkAddress, defaults);
        _uiSystem.SetUiState(configuratorUid, NetworkConfiguratorUiKey.Link, state);
    }

    /// <summary>
    /// Opens the config ui. It can be used to modify the devices in the targets device list.
    /// </summary>
    protected override void OpenDeviceListUi(
        EntityUid configuratorUid,
        EntityUid? targetUid,
        EntityUid userUid,
        NetworkConfiguratorComponent configurator)
    {
        if (configurator.ActiveDeviceLink == targetUid)
            return;

        if (Delay((configuratorUid, configurator)))
            return;

        if (!targetUid.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        if (!TryComp(targetUid, out DeviceListComponent? list))
            return;

        if (TryComp(configurator.ActiveDeviceList, out DeviceListComponent? oldList))
            oldList.Configurators.Remove(configuratorUid);

        list.Configurators.Add(configuratorUid);
        configurator.ActiveDeviceList = targetUid;
        Dirty(configuratorUid, configurator);

        if (_uiSystem.TryOpenUi(configuratorUid, NetworkConfiguratorUiKey.Configure, userUid))
        {
            _uiSystem.SetUiState(configuratorUid, NetworkConfiguratorUiKey.Configure, new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(configurator.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value).EntityName)).ToHashSet()
            ));
        }
    }

    /// <summary>
    /// Sends the list of saved devices to the ui
    /// </summary>
    private void UpdateListUiState(EntityUid uid, NetworkConfiguratorComponent component)
    {
        HashSet<(string address, string name)> devices = new();
        HashSet<string> invalidDevices = new();

        foreach (var pair in component.Devices)
        {
            if (!Exists(pair.Value))
            {
                invalidDevices.Add(pair.Key);
                continue;
            }

            devices.Add((pair.Key, Name(pair.Value)));
        }

        //Remove saved entities that don't exist anymore
        foreach (var invalidDevice in invalidDevices)
        {
            component.Devices.Remove(invalidDevice);
        }

        _uiSystem.SetUiState(uid, NetworkConfiguratorUiKey.List, new NetworkConfiguratorUserInterfaceState(devices));
    }

    /// <summary>
    /// Clears the active device list when the ui is closed
    /// </summary>
    private void OnUiClosed(EntityUid uid, NetworkConfiguratorComponent component, BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(NetworkConfiguratorUiKey.Configure)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.Link)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.List))
        {
            return;
        }

        if (TryComp(component.ActiveDeviceList, out DeviceListComponent? list))
        {
            list.Configurators.Remove(uid);
        }

        component.ActiveDeviceList = null;

        if (args.UiKey is NetworkConfiguratorUiKey.Link)
        {
            component.ActiveDeviceLink = null;
            component.DeviceLinkTarget = null;
        }
        Dirty(uid, component);
    }

    public void OnDeviceListShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<DeviceListComponent> list)
    {
        list.Comp.Configurators.Remove(conf.Owner);
        if (Resolve(conf.Owner, ref conf.Comp))
        {
            conf.Comp.ActiveDeviceList = null;
            Dirty(conf);
        }
    }

    /// <summary>
    /// Removes a device from the saved devices list
    /// </summary>
    private void OnRemoveDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorRemoveDeviceMessage args)
    {
        if (component.Devices.TryGetValue(args.Address, out var removedDevice))
        {
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                $"{ToPrettyString(args.Actor):actor} removed buffered device {ToPrettyString(removedDevice):subject} from {ToPrettyString(uid):tool}");
        }

        component.Devices.Remove(args.Address);
        if (TryComp(removedDevice, out DeviceNetworkComponent? device))
            device.Configurators.Remove(uid);

        UpdateListUiState(uid, component);
    }

    /// <summary>
    /// Clears the saved devices
    /// </summary>
    private void OnClearDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorClearDevicesMessage args)
    {
        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared buffered devices from {ToPrettyString(uid):tool}");

        ClearDevices(uid, component);
        UpdateListUiState(uid, component);
    }

    private void ClearDevices(EntityUid uid, NetworkConfiguratorComponent component)
    {
        var query = GetEntityQuery<DeviceNetworkComponent>();
        foreach (var device in component.Devices.Values)
        {
            if (query.TryGetComponent(device, out var comp))
                comp.Configurators.Remove(uid);
        }

        component.Devices.Clear();
    }

    private void OnClearLinks(EntityUid uid, NetworkConfiguratorComponent configurator, NetworkConfiguratorClearLinksMessage args)
    {
        if (!configurator.ActiveDeviceLink.HasValue || !configurator.DeviceLinkTarget.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared links between {ToPrettyString(configurator.ActiveDeviceLink.Value):subject} and {ToPrettyString(configurator.DeviceLinkTarget.Value):subject2} with {ToPrettyString(uid):tool}");

        if (HasComp<DeviceLinkSourceComponent>(configurator.ActiveDeviceLink) && HasComp<DeviceLinkSinkComponent>(configurator.DeviceLinkTarget))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value
                );

            UpdateLinkUiState(
                uid,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value
                );
        }
        else if (HasComp<DeviceLinkSourceComponent>(configurator.DeviceLinkTarget) && HasComp<DeviceLinkSinkComponent>(configurator.ActiveDeviceLink))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value
                );

            UpdateLinkUiState(
                uid,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value
                );
        }
    }

    private void OnToggleLinks(EntityUid uid, NetworkConfiguratorComponent configurator, NetworkConfiguratorToggleLinkMessage args)
    {
        if (!configurator.ActiveDeviceLink.HasValue || !configurator.DeviceLinkTarget.HasValue)
            return;

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(configurator.DeviceLinkTarget, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value,
                args.Source, args.Sink,
                activeSource, targetSink);

            UpdateLinkUiState(uid, configurator.ActiveDeviceLink.Value, configurator.DeviceLinkTarget.Value, activeSource);
        }
        else if (TryComp(configurator.DeviceLinkTarget, out DeviceLinkSourceComponent? targetSource) && TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                args.Source, args.Sink,
                targetSource, activeSink
                );

            UpdateLinkUiState(
                uid,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                targetSource
                );
        }
    }

    /// <summary>
    /// Saves links set by the device link UI
    /// </summary>
    private void OnSaveLinks(EntityUid uid, NetworkConfiguratorComponent configurator, NetworkConfiguratorLinksSaveMessage args)
    {
        if (!configurator.ActiveDeviceLink.HasValue || !configurator.DeviceLinkTarget.HasValue)
            return;

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(configurator.DeviceLinkTarget, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value,
                args.Links,
                activeSource,
                targetSink
                );

            UpdateLinkUiState(
                uid,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value,
                activeSource
                );
        }
        else if (TryComp(configurator.DeviceLinkTarget, out DeviceLinkSourceComponent? targetSource) && TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                args.Links,
                targetSource,
                activeSink
                );

            UpdateLinkUiState(
                uid,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                targetSource
                );
        }
    }

    /// <summary>
    /// Handles all the button presses from the config ui.
    /// Modifies, copies or visualizes the targets device list
    /// </summary>
    private void OnConfigButtonPressed(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorButtonPressedMessage args)
    {
        if (!component.ActiveDeviceList.HasValue)
            return;

        var result = DeviceListUpdateResult.NoComponent;
        switch (args.ButtonKey)
        {
            case NetworkConfiguratorButtonKey.Set:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} set device links to {ToPrettyString(component.ActiveDeviceList.Value):subject} with {ToPrettyString(uid):tool}");

                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values));
                break;
            case NetworkConfiguratorButtonKey.Add:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} added device links to {ToPrettyString(component.ActiveDeviceList.Value):subject} with {ToPrettyString(uid):tool}");

                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values), true);
                break;
            case NetworkConfiguratorButtonKey.Clear:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} cleared device links from {ToPrettyString(component.ActiveDeviceList.Value):subject} with {ToPrettyString(uid):tool}");
                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>());
                break;
            case NetworkConfiguratorButtonKey.Copy:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} copied devices from {ToPrettyString(component.ActiveDeviceList.Value):subject} to {ToPrettyString(uid):tool}");

                ClearDevices(uid, component);

                var query = GetEntityQuery<DeviceNetworkComponent>();
                foreach (var (addr, device) in _deviceListSystem.GetDeviceList(component.ActiveDeviceList.Value))
                {
                    if (query.TryGetComponent(device, out var comp))
                    {
                        component.Devices.Add(addr, device);
                        comp.Configurators.Add(uid);
                    }
                }
                UpdateListUiState(uid, component);
                return;
            case NetworkConfiguratorButtonKey.Show:
                break;
        }

        var resultText = result switch
        {
            DeviceListUpdateResult.TooManyDevices => Loc.GetString("network-configurator-too-many-devices"),
            DeviceListUpdateResult.UpdateOk => Loc.GetString("network-configurator-update-ok"),
            _ => "error"
        };

        _popupSystem.PopupCursor(Loc.GetString(resultText), args.Actor, PopupType.Medium);
        _uiSystem.SetUiState(
            uid,
            NetworkConfiguratorUiKey.Configure,
            new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(component.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value).EntityName)).ToHashSet()));
    }

    public void OnDeviceShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<DeviceNetworkComponent> device)
    {
        device.Comp.Configurators.Remove(conf.Owner);
        if (!Resolve(conf.Owner, ref conf.Comp))
            return;

        foreach (var (addr, dev) in conf.Comp.Devices)
        {
            if (device.Owner == dev)
                conf.Comp.Devices.Remove(addr);
        }

        UpdateListUiState(conf, conf.Comp);
    }
    #endregion
}
