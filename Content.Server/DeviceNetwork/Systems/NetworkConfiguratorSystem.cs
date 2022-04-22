using System.Collections.Immutable;
using Content.Server.DeviceNetwork.Components;
using Content.Server.UserInterface;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.DeviceNetwork.Systems;

[UsedImplicitly]
public sealed class NetworkConfiguratorSystem : EntitySystem
{
    [Dependency] private readonly DeviceListSystem _deviceListSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        //Interaction
        SubscribeLocalEvent<NetworkConfiguratorComponent, UseInHandEvent>(OnUsedInHand);
        SubscribeLocalEvent<NetworkConfiguratorComponent, AfterInteractEvent>((uid, component, args) => OnUsed(uid, component, args.Target, args.User, args.CanReach)); //TODO: Replace with utility verb?

        //Verbs
        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<UtilityVerb>>(OnAddInteractVerb);
        SubscribeLocalEvent<DeviceNetworkComponent, GetVerbsEvent<AlternativeVerb>>(OnAddAlternativeSaveDeviceVerb);

        //UI
        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorRemoveDeviceMessage>(OnRemoveDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearDevicesMessage>(OnClearDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorButtonPressedMessage>(OnConfigButtonPressed);
    }

    public void TryAddNetworkDevice(EntityUid? targetUid, EntityUid configuratorUid, EntityUid userUid,
        NetworkConfiguratorComponent? configurator = null)
    {
        if (!Resolve(configuratorUid, ref configurator))
            return;

        TryAddNetworkDevice(targetUid, userUid, configurator);
    }

    private void TryAddNetworkDevice(EntityUid? targetUid, EntityUid userUid, NetworkConfiguratorComponent configurator, DeviceNetworkComponent? device = null)
    {
        if (!targetUid.HasValue || !Resolve(targetUid.Value, ref device))
            return;

        if (string.IsNullOrEmpty(device.Address))
        {
            _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-failed", ("device", targetUid)), Filter.Entities(userUid));
            return;
        }

        if (configurator.Devices.ContainsValue(targetUid.Value))
        {
            _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-already-saved", ("device", targetUid)), Filter.Entities(userUid));
            return;
        }

        configurator.Devices.Add(device.Address, targetUid.Value);
        _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-saved", ("address", device.Address), ("device", targetUid)),
            Filter.Entities(userUid));

        UpdateUiState(configurator.Owner, configurator);
    }

    #region Interactions

    /// <summary>
    /// Shows the saved devices ui. It can be used to view and remove saved devices
    /// </summary>
    private void OnUsedInHand(EntityUid uid, NetworkConfiguratorComponent component, UseInHandEvent args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        _uiSystem.GetUiOrNull(uid, NetworkConfiguratorUiKey.List)?.Open(actor.PlayerSession);
        UpdateUiState(uid, component);
    }

    /// <summary>
    /// Either adds a device to the device list or shows the config ui if the target is ant entity with a device list
    /// </summary>
    private void OnUsed(EntityUid uid, NetworkConfiguratorComponent component, EntityUid? target, EntityUid user, bool canReach = true)
    {
        if (!canReach)
            return;

        if (!HasComp<DeviceListComponent>(target))
        {
            TryAddNetworkDevice(target, user, component);
            return;
        }

        OpenDeviceListUi(target, user, component);
    }

    #endregion

    #region Verbs

    /// <summary>
    /// Adds the interaction verb which is either configuring device lists or saving a device onto the configurator
    /// </summary>
    private void OnAddInteractVerb(EntityUid uid, NetworkConfiguratorComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue)
            return;

        var isDeviceList = HasComp<DeviceListComponent>(args.Target);

        UtilityVerb verb = new()
        {
            Text = Loc.GetString(isDeviceList ? "network-configurator-configure" : "network-configurator-save-device"),
            IconTexture = isDeviceList ? "/Textures/Interface/VerbIcons/settings.svg.192dpi.png" : "/Textures/Interface/VerbIcons/in.svg.192dpi.png",
            Act = () => OnUsed(uid, component, args.Target, args.User),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Powerful. Funny alt interact using.
    /// Adds an alternative verb for saving a device on the configurator for entities with the <see cref="DeviceListComponent"/>.
    /// Allows alt clicking entities with a network configurator that would otherwise trigger a different action like entities
    /// with a <see cref="DeviceListComponent"/>
    /// </summary>
    private void OnAddAlternativeSaveDeviceVerb(EntityUid uid, DeviceNetworkComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<NetworkConfiguratorComponent>(args.Using.Value)
            || !HasComp<DeviceListComponent>(args.Target))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("network-configurator-save-device"),
            IconTexture = "/Textures/Interface/VerbIcons/in.svg.192dpi.png",
            Act = () => TryAddNetworkDevice(args.Target, args.Using.Value, args.User),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    #endregion

    #region UI

    /// <summary>
    /// Opens the config ui. It can be used to modify the devices in the targets device list.
    /// </summary>
    private void OpenDeviceListUi(EntityUid? targetUid, EntityUid userUid, NetworkConfiguratorComponent configurator)
    {
        if (!targetUid.HasValue || !TryComp(userUid, out ActorComponent? actor))
            return;

        configurator.ActiveDeviceList = targetUid;
        _uiSystem.GetUiOrNull(configurator.Owner, NetworkConfiguratorUiKey.Configure)?.Open(actor.PlayerSession);
    }

    /// <summary>
    /// Sends the list of saved devices to the ui
    /// </summary>
    private void UpdateUiState(EntityUid uid, NetworkConfiguratorComponent component)
    {
        HashSet<(string address, string name)> devices = new();

        foreach (var pair in component.Devices)
        {
            devices.Add((pair.Key, Name(pair.Value)));
        }

        _uiSystem.GetUiOrNull(uid, NetworkConfiguratorUiKey.List)?.SetState(new NetworkConfiguratorUserInterfaceState(devices));
    }

    /// <summary>
    /// Clears the active device list when the ui is closed
    /// </summary>
    private void OnUiClosed(EntityUid uid, NetworkConfiguratorComponent component, BoundUIClosedEvent args)
    {
        component.ActiveDeviceList = null;
    }

    /// <summary>
    /// Removes a device from the saved devices list
    /// </summary>
    private void OnRemoveDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorRemoveDeviceMessage args)
    {
        component.Devices.Remove(args.Address);
        UpdateUiState(uid, component);
    }

    /// <summary>
    /// Clears the saved devices
    /// </summary>
    private void OnClearDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorClearDevicesMessage _)
    {
        component.Devices.Clear();
        UpdateUiState(uid, component);
    }

    /// <summary>
    /// Handles all the button presses from the config ui.
    /// Modifies, copies or visualizes the targets device list
    /// </summary>
    private void OnConfigButtonPressed(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorButtonPressedMessage args)
    {
        if (!component.ActiveDeviceList.HasValue)
            return;

        switch (args.ButtonKey)
        {
            case NetworkConfiguratorButtonKey.Set:
                _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values));
                break;
            case NetworkConfiguratorButtonKey.Add:
                _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values), true);
                break;
            case NetworkConfiguratorButtonKey.Clear:
                _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>());
                break;
            case NetworkConfiguratorButtonKey.Copy:
                component.Devices = _deviceListSystem.GetDeviceList(component.ActiveDeviceList.Value);
                UpdateUiState(uid, component);
                break;
            case NetworkConfiguratorButtonKey.Show:
                _deviceListSystem.ToggleVisualization(component.ActiveDeviceList.Value);
                break;
        }
    }

    #endregion
}
