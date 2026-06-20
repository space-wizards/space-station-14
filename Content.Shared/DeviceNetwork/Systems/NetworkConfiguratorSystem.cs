using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class NetworkConfiguratorSystem : EntitySystem
{
    [Dependency] private DeviceListSystem _deviceListSystem = default!;
    [Dependency] private DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private AccessReaderSystem _accessSystem = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetworkConfiguratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NetworkConfiguratorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUserInterfaceCheckRangeEvent>(OnUiRangeCheck);
        SubscribeLocalEvent<NetworkConfiguratorComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);

        SubscribeLocalEvent<DeviceListComponent, ComponentRemove>(OnComponentRemoved);

        SubscribeLocalEvent<BeforeSerializationEvent>(OnMapSave);

        InitializeVerb();
        InitializeUI();
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

    private void OnUiOpenAttempt(Entity<NetworkConfiguratorComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.LinkModeActive)
            args.Cancel();
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
        ClearDevices(ent);

        if (TryComp(ent.Comp.ActiveDeviceList, out DeviceListComponent? list))
            list.Configurators.Remove(ent);

        ent.Comp.ActiveDeviceList = null;
    }

    private void OnMapInit(Entity<NetworkConfiguratorComponent> ent, ref MapInitEvent args)
    {
        UpdateListUiState(ent);
    }

    private void TryAddNetworkDevice(Entity<NetworkConfiguratorComponent?> configurator, Entity<DeviceNetworkComponent?> target, EntityUid userUid)
    {
        if (!Resolve(configurator.Owner, ref configurator.Comp)
            || !Resolve(target.Owner, ref target.Comp, false))
            return;

        //This checks if the device is marked as having a savable address,
        //to avoid adding pdas and whatnot to air alarms. This flag is true
        //by default, so this will only prevent devices from being added to
        //network configurator lists if manually set to false in the prototype
        if (!target.Comp.SavableAddress)
            return;

        var address = target.Comp.Address;
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
            if (MetaData(target).EntityLifeStage == EntityLifeStage.MapInitialized)
            {
                _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-failed", ("device", target)),
                    userUid);
                return;
            }

            address = $"UID: {target}";
        }

        if (configurator.Comp.Devices.ContainsValue(target))
        {
            _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-already-saved", ("device", target)), userUid);
            return;
        }

        target.Comp.Configurators.Add(configurator);
        configurator.Comp.Devices.Add(address, target);
        _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-saved", ("address", target.Comp.Address), ("device", target)),
            userUid,
            PopupType.Medium);

        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userUid):actor} saved {ToPrettyString(target):subject} to {ToPrettyString(configurator):tool}");

        UpdateListUiState(configurator!);
    }

    private void TryLinkDevice(Entity<NetworkConfiguratorComponent> configurator, EntityUid? target, EntityUid user)
    {
        if (!HasComp<DeviceLinkSourceComponent>(target) && !HasComp<DeviceLinkSinkComponent>(target))
            return;

        if (configurator.Comp.ActiveDeviceLink == target)
        {
            _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-stopped"), target.Value, user);
            configurator.Comp.ActiveDeviceLink = null;
            return;
        }

        if (configurator.Comp.ActiveDeviceLink.HasValue
            && (HasComp<DeviceLinkSourceComponent>(target)
            && HasComp<DeviceLinkSinkComponent>(configurator.Comp.ActiveDeviceLink)
            || HasComp<DeviceLinkSinkComponent>(target)
            && HasComp<DeviceLinkSourceComponent>(configurator.Comp.ActiveDeviceLink)))
        {
            OpenDeviceLinkUi(configurator, target, user);
            return;
        }

        if (HasComp<DeviceLinkSourceComponent>(target) && HasComp<DeviceLinkSourceComponent>(configurator.Comp.ActiveDeviceLink)
            || HasComp<DeviceLinkSinkComponent>(target) && HasComp<DeviceLinkSinkComponent>(configurator.Comp.ActiveDeviceLink))
            return;

        _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-started", ("device", Name(target.Value))), target.Value, user);
        configurator.Comp.ActiveDeviceLink = target;
    }

    private void TryLinkDefaults(EntityUid _, NetworkConfiguratorComponent configurator, EntityUid? targetUid, EntityUid user)
    {
        if (!configurator.LinkModeActive || !configurator.ActiveDeviceLink.HasValue
            || !targetUid.HasValue || configurator.ActiveDeviceLink == targetUid)
            return;

        if (!HasComp<DeviceLinkSourceComponent>(targetUid) && !HasComp<DeviceLinkSinkComponent>(targetUid))
            return;

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(targetUid, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.LinkDefaults(user,
                (configurator.ActiveDeviceLink.Value, activeSource),
                (targetUid.Value, targetSink));
        }
        else if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink) && TryComp(targetUid, out DeviceLinkSourceComponent? targetSource))
        {
            _deviceLinkSystem.LinkDefaults(user,
                (targetUid.Value, targetSource),
                (configurator.ActiveDeviceLink.Value, activeSink));
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

        if (!userUid.HasValue)
            return;

        if (!configurator.Comp.LinkModeActive)
            configurator.Comp.ActiveDeviceLink = null;

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
    }

    /// <summary>
    /// Updates the configurators appearance and plays a sound indicating that the mode switched
    /// </summary>
    private void UpdateModeAppearance(EntityUid userUid, Entity<NetworkConfiguratorComponent> configurator)
    {
        Dirty(configurator);
        _appearanceSystem.SetData(configurator.Owner, NetworkConfiguratorVisuals.Mode, configurator.Comp.LinkModeActive);

        var pitch = configurator.Comp.LinkModeActive ? 1 : 0.8f;
        _audioSystem.PlayPvs(configurator.Comp.SoundSwitchMode, userUid, AudioParams.Default.WithVolume(1.5f).WithPitchScale(pitch));
    }

    /// <summary>
    /// Returns true if the last time this method was called is earlier than the configurators use delay.
    /// </summary>
    private bool Delay(NetworkConfiguratorComponent configurator)
    {
        var currentTime = _gameTiming.CurTime;
        if (currentTime < configurator.LastUseAttempt + configurator.UseDelay)
            return true;

        configurator.LastUseAttempt = currentTime;
        return false;
    }
}
