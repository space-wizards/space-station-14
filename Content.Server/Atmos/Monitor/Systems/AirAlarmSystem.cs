using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.Atmos.Monitor.Systems
{
    // AirAlarm system - specific for atmos devices, rather than
    // atmos monitors.
    //
    // oh boy, message passing!
    //
    // Commands should always be sent into packet's Command
    // data key. In response, a packet will be transmitted
    // with the response type as its command, and the
    // response data in its data key.
    public class AirAlarmSystem : EntitySystem
    {
        [Dependency] private readonly DeviceNetworkSystem _deviceNet = default!;
        [Dependency] private readonly AtmosMonitorSystem _atmosMonitorSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public const int Freq = AtmosMonitorSystem.AtmosMonitorApcFreq;

        // -- Atmos Device Commands --

        // Toggles the device on or off.
        //
        // Disabled. Sync/set device data should do this already.
        // public const string AirAlarmToggleCmd = "air_alarm_toggle_device";

        // Gets the type of atmos device.
        //
        // Disabled, since sync should technically get this already.
        //
        // public const string AirAlarmGetType = "air_alarm_get_type";

        // Sets the data on the atmos device. This can be any object,
        // and as long as the type matches to the data, it will
        // succeed. This is both a command and data key.
        public const string AirAlarmSetData = "air_alarm_set_device_data";

        // Request a sync from the devices in the network.
        public const string AirAlarmSyncCmd = "air_alarm_sync_devices";

        // Sets the air alarm's program.
        public const string AirAlarmSetMode = "air_alarm_set_mode";

        // -- Packet Data --

        // ToggleData. This should just echo what the device's
        // enabled/disabled state is.
        //
        // Disabled. This should also be in set_device_data.
        // public const string AirAlarmToggleData = "air_alarm_toggle_device_data";

        // TypeData. This should hold the type of device that it is,
        // so that the UI knows what kind of widget to draw, or
        // to ensure that the data being sent is considered
        // correct.
        //
        // Disabled. This should be in the sync data packet.
        // public const string AirAlarmTypeData = "air_alarm_type_data";

        // SetDataStatus. This should be a bool
        // that returns true if the op succeeded,
        // or false if it failed.
        public const string AirAlarmSetDataStatus = "air_alarm_set_device_data_status";

        // Sync data. This could be a number of things, depending
        // on the atmos devices in the network. A successful sync
        // will have this as the packet command, with data
        // keyed by this string containing all the sync data required.
        //
        // (fyi this might be literal component data passed through)
        public const string AirAlarmSyncData = "air_alarm_device_sync_data";

        // -- API --

        public void SetData(EntityUid uid, string address, IAtmosDeviceData data)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSetData,
                // [AirAlarmTypeData] = type,
                [AirAlarmSetData] = data
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }

        public void SyncAllDevices(EntityUid uid)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSyncCmd
            };

            _deviceNet.QueuePacket(uid, string.Empty, Freq, payload, true);
        }

        public void SyncDevice(EntityUid uid, string address)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSyncCmd
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }

        public void SyncMode(EntityUid uid, AirAlarmMode mode)
        {
            var payload = new NetworkPayload
            {
                    [DeviceNetworkConstants.Command] = AirAlarmSetMode,
                    [AirAlarmSetMode] = mode
            };

            _deviceNet.QueuePacket(uid, string.Empty, Freq, payload, true);
        }

        /*
        public void ToggleDevice(EntityUid uid, string address, bool toggle)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmToggleCmd,
                [AirAlarmToggleCmd] = toggle
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }
        */

        // -- Internal --

        public override void Initialize()
        {
            SubscribeLocalEvent<AirAlarmComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<AirAlarmComponent, PacketSentEvent>(OnPacketRecv);
            SubscribeLocalEvent<AirAlarmComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<AirAlarmComponent, AirAlarmUpdateAlarmModeMessage>(OnUpdateAlarmMode);
            SubscribeLocalEvent<AirAlarmComponent, AirAlarmUpdateAlarmThresholdMessage>(OnUpdateThreshold);
            SubscribeLocalEvent<AirAlarmComponent, AirAlarmUpdateDeviceDataMessage>(OnUpdateDeviceData);
            SubscribeLocalEvent<AirAlarmComponent, BoundUIClosedEvent>(OnClose);
            SubscribeLocalEvent<AirAlarmComponent, InteractHandEvent>(OnInteract);
        }

        private void OnClose(EntityUid uid, AirAlarmComponent component, BoundUIClosedEvent args)
        {
            component.ActivePlayers.Remove(args.Session.UserId);
            if (component.ActivePlayers.Count == 0)
                RemoveActiveInterface(uid);
        }

        private void OnInteract(EntityUid uid, AirAlarmComponent component, InteractHandEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.Owner.GetUIOrNull(SharedAirAlarmInterfaceKey.Key)?.Open(actor.PlayerSession);
            component.ActivePlayers.Add(actor.PlayerSession.UserId);
            AddActiveInterface(uid);
            SendAlarmMode(uid);
            SendThresholds(uid);
            SyncAllDevices(uid);
            SendAirData(uid);
        }

        private void OnUpdateAlarmMode(EntityUid uid, AirAlarmComponent component, AirAlarmUpdateAlarmModeMessage args) =>
            SetMode(uid, args.Mode);

        private void OnUpdateThreshold(EntityUid uid, AirAlarmComponent component, AirAlarmUpdateAlarmThresholdMessage args) =>
            SetThreshold(uid, args.Threshold, args.Type, args.Gas);

        private void OnUpdateDeviceData(EntityUid uid, AirAlarmComponent component, AirAlarmUpdateDeviceDataMessage args) =>
            SetDeviceData(uid, args.Address, args.Data);

        // tempted to make air alarm modes just broadcast packets that the
        // scrubbers/filters themselves process, instead of
        // doing a filter/type switch on saved device data
        //
        // that way air alarms can blindly set modes without having
        // to sync against all devices :HECK:
        //
        // this also creates the issue, however, that anyone who
        // accesses the network can simply send a single broadcast
        // and cause a room to siphon without even touching the
        // air alarm (atmos devices should have only a 'panic'
        // switch packet-wise, which does a predetermined thing, and not
        // some too-simple thing that could potentially be
        // abusable)
        //
        // so instead we'll just use a raised targetted event to mimic
        // the panic mode on a vent/scrubber
        //
        // TODO: consolidate all the data into one component, holy shit lmao

        private void OnComponentStartup(EntityUid uid, AirAlarmComponent component, ComponentStartup args)
        {
        }

        private void OnAtmosAlarm(EntityUid uid, AirAlarmComponent component, AtmosMonitorAlarmEvent args)
        {
            if (component.ActivePlayers.Count != 0)
            {
                SyncAllDevices(uid);
                SendAirData(uid);
            }

            if (args.HighestNetworkType == AtmosMonitorAlarmType.Danger)
            {

                // _airAlarmDataSystem.UpdateAlarmMode(uid, AirAlarmMode.None);

                // data.CurrentMode = AirAlarmMode.None;
                // data.DirtyMode = true;
                // data.Dirty();
                SetMode(uid, AirAlarmMode.None);
                // set mode to off to mimic the vents/scrubbers being turned off
                // update UI
                //
                // no, the mode isn't processed here - it's literally just
                // set to what mimics 'off'
            }
            else if (args.HighestNetworkType == AtmosMonitorAlarmType.Normal)
            {
                // if the mode is still set to off, set it to filtering instead
                // alternatively, set it to the last saved mode
                //
                // no, this still doesn't execute the mode
                // _airAlarmDataSystem.UpdateAlarmMode(uid, AirAlarmMode.Filtering);
                // data.CurrentMode = AirAlarmMode.Filtering;
                // data.DirtyMode = true;
                // data.Dirty();
                SetMode(uid, AirAlarmMode.Filtering);
            }
        }

        public void SetThreshold(EntityUid uid, AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;

            _atmosMonitorSystem.SetThreshold(uid, type, threshold, gas);

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(type, threshold, gas));

            /*
            switch (type)
            {
                case AtmosMonitorThresholdType.Pressure:
                    data.PressureThreshold = threshold;
                    data.DirtyThresholds.Add(AtmosMonitorThresholdType.Pressure);
                    break;
                case AtmosMonitorThresholdType.Temperature:
                    data.TemperatureThreshold = threshold;
                    data.DirtyThresholds.Add(AtmosMonitorThresholdType.Temperature);
                    break;
                case AtmosMonitorThresholdType.Gas:
                    data.DirtyThresholds.Add(AtmosMonitorThresholdType.Gas);
                    data.GasThresholds[(Gas) gas!] = threshold;
                    break;
            }

            data.Dirty();

            controller.UpdateUI();
            */
        }

        public void SetMode(EntityUid uid, AirAlarmMode mode, bool noSync = false, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;
            controller.CurrentMode = mode;
            // controller.SendMessage(new AirAlarmUpdateAlarmModeMessage(mode));
            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmModeMessage(mode));

            if (!noSync) SyncMode(uid, mode);
            /*
            Logger.DebugS("AirAlarmData", "Dirty air alarm mode detected.");
            Logger.DebugS("AirAlarmData", $"CurrentMode: {data.CurrentMode}");
            Logger.DebugS("AirAlarmData", $"DirtyMode: {data.DirtyMode}");
            */

            /*
            foreach (var (addr, device) in data.DeviceData)
            {
                switch (device)
                {
                    case GasVentPumpData ventDevice:
                        break;
                    case GasVentScrubberData scrubberDevice:
                        break;
                }
            }
            // TODO: Mode setting/programs.
            // */
            /*
            data.CurrentMode = mode;
            data.DirtyMode = true;
            data.Dirty();

            controller.UpdateUI();
            */
        }

        public void SetDeviceData(EntityUid uid, string address, IAtmosDeviceData devData, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;

            SetData(uid, address, devData);

            /*
            data.DeviceData[address] = devData;
            data.DirtyDevices.Add(address);
            data.Dirty();

            controller.UpdateUI();
            */
        }

        private void OnPacketRecv(EntityUid uid, AirAlarmComponent controller, PacketSentEvent args)
        {
            Logger.DebugS("AirAlarmSystem", $"Received packet from {args.SenderAddress}");
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd))
                return;

            switch (cmd)
            {
                case AirAlarmSyncData:
                    if (!args.Data.TryGetValue(AirAlarmSyncData, out IAtmosDeviceData? data)
                        || data == null) break;

                    // Save into component.
                    // Sync data to interface.
                    // _airAlarmDataSystem.UpdateDeviceData(uid, args.SenderAddress, data);
                    //
                    /*
                    alarmData.DeviceData[args.SenderAddress] = data;
                    alarmData.DirtyDevices.Add(args.SenderAddress);
                    alarmData.Dirty();

                    controller.UpdateUI();
                    */

                    _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateDeviceDataMessage(args.SenderAddress, data));

                    return;
                case AirAlarmSetDataStatus:
                    if (!args.Data.TryGetValue(AirAlarmSetDataStatus, out bool dataStatus)) break;

                    // Sync data to interface.
                    // This should say if the result
                    // failed, or succeeded. Don't save it.l
                    SyncDevice(uid, args.SenderAddress);

                    return;
                case AirAlarmSetMode:
                    if (!args.Data.TryGetValue(AirAlarmSetMode, out AirAlarmMode alarmMode)) break;

                    SetMode(uid, alarmMode, true);

                    return;
            }
        }

        // <-- UI stuff -->

        // List of active user interfaces.
        private HashSet<EntityUid> _activeUserInterfaces = new();

        // Add an active interface for updating.
        public void AddActiveInterface(EntityUid uid)
        {
            _activeUserInterfaces.Add(uid);
        }

        // Remove an active interface from updating.
        public void RemoveActiveInterface(EntityUid uid)
        {
            _activeUserInterfaces.Remove(uid);
        }

        // Update an interface's air data. This is all the 'hot' data
        // that an air alarm contains server-side. Updated with a whopping 16
        // delay automatically once a UI is in the loop.
        public void SendAirData(EntityUid uid, AirAlarmComponent? alarm = null, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null)
        {
            if (!Resolve(uid, ref alarm, ref monitor, ref power)) return;

            if (!power.Powered) return;


            if (monitor.TileGas != null)
            {
                var gases = new Dictionary<Gas, float>();

                foreach (var gas in Enum.GetValues<Gas>())
                    gases.Add(gas, monitor.TileGas.GetMoles(gas));

                var airData = new AirAlarmAirData(monitor.TileGas.Pressure, monitor.TileGas.Temperature, monitor.TileGas.TotalMoles, monitor.LastAlarmState, gases);

                Logger.DebugS("AirAlarmSystem", "Attempting to update data now.");

                // _airAlarmDataSystem.UpdateAirData(uid, data);
                /*
                data.AirData = airData;
                data.Dirty();
                */

                _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAirDataMessage(airData));
            }

            // alarm.UpdateUI();
        }

        public void SendAlarmMode(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref controller)
                || !power.Powered) return;

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmModeMessage(controller.CurrentMode));

            // _airAlarmDataSystem.UpdateAlarmMode(uid, data.CurrentMode);
            // data.DirtyMode = true;
            // data.Dirty();
        }

        public void SendThresholds(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref controller)
                || !power.Powered) return;

            if (monitor.PressureThreshold == null
                && monitor.TemperatureThreshold == null
                && monitor.GasThresholds == null)
                return;

            if (monitor.PressureThreshold != null)
            {
                // _airAlarmDataSystem.UpdateAlarmThreshold(uid, monitor.PressureThreshold, AtmosMonitorThresholdType.Pressure);
                // data.PressureThreshold = monitor.PressureThreshold;
                // data.DirtyThresholds.Add(AtmosMonitorThresholdType.Pressure);
                _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Pressure, monitor.PressureThreshold));
            }

            if (monitor.TemperatureThreshold != null)
            {
                _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Temperature, monitor.TemperatureThreshold));
                // _airAlarmDataSystem.UpdateAlarmThreshold(uid, monitor.TemperatureThreshold, AtmosMonitorThresholdType.Temperature);
                // data.TemperatureThreshold = monitor.TemperatureThreshold;
                // data.DirtyThresholds.Add(AtmosMonitorThresholdType.Temperature);
            }

            if (monitor.GasThresholds != null)
            {
                // data.DirtyThresholds.Add(AtmosMonitorThresholdType.Gas);
                foreach (var (gas, threshold) in monitor.GasThresholds)
                    _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Gas, threshold, gas));
                    // _airAlarmDataSystem.UpdateAlarmThreshold(uid, threshold, AtmosMonitorThresholdType.Gas, gas);
                    // data.GasThresholds[gas] = threshold;
            }
        }

        private const float _delay = 16f;
        private float _timer = 0f;

        public override void Update(float frameTime)
        {
            _timer += frameTime;
            if (_timer >= _delay)
            {
                _timer = 0f;
                foreach (var uid in _activeUserInterfaces)
                    SendAirData(uid);
            }
        }
    }
}
