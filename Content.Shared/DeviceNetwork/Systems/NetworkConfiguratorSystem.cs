using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class NetworkConfiguratorSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private DeviceListSystem _deviceListSystem = default!;
    [Dependency] private DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery = default!;
    [Dependency] private EntityQuery<DeviceLinkSinkComponent> _deviceLinkSinkQuery = default!;
    [Dependency] private EntityQuery<DeviceLinkSourceComponent> _deviceLinkSourceQuery = default!;
    [Dependency] private EntityQuery<DeviceListComponent> _deviceListQuery = default!;
    [Dependency] private EntityQuery<NetworkConfiguratorComponent> _networkConfigQuery = default!;

    [SubscribeLocalEvent]
    private void OnMapSave(BeforeSerializationEvent ev)
    {
        var enumerator = AllEntityQuery<NetworkConfiguratorComponent>();
        while (enumerator.MoveNext(out var uid, out var conf))
        {
            if (conf.ActiveDeviceList == null || TerminatingOrDeleted(conf.ActiveDeviceList))
                continue;

            var listXform = Transform(conf.ActiveDeviceList.Value);

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

    [SubscribeLocalEvent]
    private void OnUiOpenAttempt(Entity<NetworkConfiguratorComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.LinkModeActive)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnUiRangeCheck(Entity<NetworkConfiguratorComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (ent.Comp.ActiveDeviceList == null || args.Result == BoundUserInterfaceRangeResult.Fail)
            return;

        DebugTools.Assert(Exists(ent.Comp.ActiveDeviceList));
        if (!_interactionSystem.InRangeUnobstructed(args.Actor!, ent.Comp.ActiveDeviceList.Value))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<NetworkConfiguratorComponent> ent, ref ComponentShutdown args)
    {
        ClearDevices(ent);

        if (_deviceListQuery.TryComp(ent.Comp.ActiveDeviceList, out var list))
            list.Configurators.Remove(ent);

        ent.Comp.ActiveDeviceList = null;
        DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceList));
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<NetworkConfiguratorComponent> ent, ref MapInitEvent args)
    {
        UpdateListUiState(ent);
    }

    private void TryAddNetworkDevice(Entity<NetworkConfiguratorComponent?> configurator, Entity<DeviceNetworkComponent?> target, EntityUid userUid)
    {
        if (!_networkConfigQuery.Resolve(configurator.Owner, ref configurator.Comp)
            || !_deviceNetworkQuery.Resolve(target.Owner, ref target.Comp, false))
            return;

        //This checks if the device is marked as having a savable address,
        //to avoid adding pdas and whatnot to air alarms. This flag is true
        //by default, so this will only prevent devices from being added to
        //network configurator lists if manually set to false in the prototype
        if (!target.Comp.SavableAddress)
            return;

        var address = _deviceNetwork.GetAddress(target);
        var addressId = target.Comp.Data.AddressId;
        if (target.Comp.Data.AddressId == 0)
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
            if (MetaData(target).EntityLifeStage == EntityLifeStage.MapInitialized)
            {
                _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-failed", ("device", target)),
                    userUid);
                return;
            }

            address = $"UID: {target}";
            addressId = new DeviceAddress(target.Owner.Id); // Weird but works for me.
        }

        if (configurator.Comp.Devices.ContainsValue(target))
        {
            _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-already-saved", ("device", target)), userUid);
            return;
        }

        target.Comp.Configurators.Add(configurator);
        configurator.Comp.Devices.Add(addressId, target);
        DirtyField(target, nameof(DeviceNetworkComponent.Configurators));
        DirtyField(configurator, nameof(NetworkConfiguratorComponent.Devices));

        _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-saved", ("address", address), ("device", target)),
            userUid,
            PopupType.Medium);

        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userUid):actor} saved {ToPrettyString(target):subject} to {ToPrettyString(configurator):tool}");

        UpdateListUiState(configurator!);
    }

    private void TryLinkDevice(Entity<NetworkConfiguratorComponent> configurator, EntityUid? target, EntityUid user)
    {
        if (!_deviceLinkSourceQuery.HasComp(target) && !_deviceLinkSinkQuery.HasComp(target))
            return;

        if (configurator.Comp.ActiveDeviceLink == target)
        {
            _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-stopped"), target.Value, user);
            configurator.Comp.ActiveDeviceLink = null;
            return;
        }

        if (configurator.Comp.ActiveDeviceLink.HasValue
            && (_deviceLinkSourceQuery.HasComp(target)
                && _deviceLinkSinkQuery.HasComp(configurator.Comp.ActiveDeviceLink)
                || _deviceLinkSinkQuery.HasComp(target)
                && _deviceLinkSourceQuery.HasComp(configurator.Comp.ActiveDeviceLink)))
        {
            OpenDeviceLinkUi(configurator, target, user);
            return;
        }

        if (_deviceLinkSourceQuery.HasComp(target) && _deviceLinkSourceQuery.HasComp(configurator.Comp.ActiveDeviceLink)
            || _deviceLinkSinkQuery.HasComp(target) && _deviceLinkSinkQuery.HasComp(configurator.Comp.ActiveDeviceLink))
            return;

        _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-started", ("device", Name(target.Value))), target.Value, user);
        configurator.Comp.ActiveDeviceLink = target;
        DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceLink));
    }

    private void TryLinkDefaults(Entity<NetworkConfiguratorComponent> configurator, EntityUid? targetUid, EntityUid user)
    {
        if (!configurator.Comp.LinkModeActive || !configurator.Comp.ActiveDeviceLink.HasValue
            || !targetUid.HasValue || configurator.Comp.ActiveDeviceLink == targetUid)
            return;

        if (!_deviceLinkSourceQuery.HasComp(targetUid) && !_deviceLinkSinkQuery.HasComp(targetUid))
            return;

        if (_deviceLinkSourceQuery.TryComp(configurator.Comp.ActiveDeviceLink, out var activeSource)
            && _deviceLinkSinkQuery.TryComp(targetUid, out var targetSink))
        {
            _deviceLinkSystem.LinkDefaults(user,
                (configurator.Comp.ActiveDeviceLink.Value, activeSource),
                (targetUid.Value, targetSink));
        }
        else if (_deviceLinkSinkQuery.TryComp(configurator.Comp.ActiveDeviceLink, out var activeSink)
                 && _deviceLinkSourceQuery.TryComp(targetUid, out var targetSource))
        {
            _deviceLinkSystem.LinkDefaults(user,
                (targetUid.Value, targetSource),
                (configurator.Comp.ActiveDeviceLink.Value, activeSink));
        }
    }

    private bool AccessCheck(EntityUid target, EntityUid? user, Entity<NetworkConfiguratorComponent> configurator)
    {
        if (user == null)
            return true;

        if (_accessSystem.IsAllowed(user.Value, target))
            return true;

        var audioParams = configurator.Comp.SoundNoAccess.Params;
        audioParams = audioParams.AddVolume(-2f).WithPitchScale(1.2f);
        _audioSystem.PlayPvs(configurator.Comp.SoundNoAccess, user.Value, audioParams);
        _popupSystem.PopupEntity(Loc.GetString("network-configurator-device-access-denied"), target, user.Value);

        return false;
    }

    [SubscribeLocalEvent]
    private void OnComponentRemoved(Entity<DeviceListComponent> ent, ref ComponentRemove args)
    {
        _uiSystem.CloseUi(ent.Owner, NetworkConfiguratorUiKey.Configure);
    }

    /// <summary>
    /// Toggles between linking and listing mode
    /// </summary>
    private void SwitchMode(EntityUid? userUid, Entity<NetworkConfiguratorComponent> configurator)
    {
        if (Delay(configurator))
            return;

        configurator.Comp.LinkModeActive = !configurator.Comp.LinkModeActive;
        DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.LinkModeActive));

        if (!userUid.HasValue)
            return;

        if (!configurator.Comp.LinkModeActive)
        {
            configurator.Comp.ActiveDeviceLink = null;
            DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceLink));
        }

        UpdateModeAppearance(userUid.Value, configurator);
    }

    /// <summary>
    /// Sets the mode to linking or list depending on the link mode parameter
    /// </summary>>
    private void SetMode(Entity<NetworkConfiguratorComponent> configurator, EntityUid userUid, bool linkMode)
    {
        configurator.Comp.LinkModeActive = linkMode;

        if (!linkMode)
            configurator.Comp.ActiveDeviceLink = null;

        UpdateModeAppearance(userUid, configurator);
        DirtyFields(configurator.AsNullable(), null, nameof(NetworkConfiguratorComponent.LinkModeActive), nameof(NetworkConfiguratorComponent.ActiveDeviceLink));
    }

    /// <summary>
    /// Updates the configurators appearance and plays a sound indicating that the mode switched
    /// </summary>
    private void UpdateModeAppearance(EntityUid userUid, Entity<NetworkConfiguratorComponent> configurator)
    {
        Dirty(configurator);
        _appearanceSystem.SetData(configurator.Owner, NetworkConfiguratorVisuals.Mode, configurator.Comp.LinkModeActive);

        var pitch = configurator.Comp.LinkModeActive ? 1 : 0.8f;
        var audioParams = configurator.Comp.SoundSwitchMode.Params;
        audioParams = audioParams.AddVolume(1.5f).WithPitchScale(pitch);
        _audioSystem.PlayPvs(configurator.Comp.SoundSwitchMode, userUid, audioParams);
    }

    /// <summary>
    /// Returns true if the last time this method was called is earlier than the configurators use delay.
    /// </summary>
    private bool Delay(Entity<NetworkConfiguratorComponent> configurator)
    {
        var currentTime = _gameTiming.CurTime;
        if (currentTime < configurator.Comp.LastUseAttempt + configurator.Comp.UseDelay)
            return true;

        configurator.Comp.LastUseAttempt = currentTime;
        DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.LastUseAttempt));
        return false;
    }
}
