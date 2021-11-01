using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Monitor.Systems;
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
        [Dependency] private readonly AirAlarmDataSystem _airAlarmDataSystem = default!;

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
            // SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmSetThresholdEvent>(OnSetThreshold);
            // SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmSetModeEvent>(OnSetMode);
            // SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmDeviceDataUpdateEvent>(OnDeviceDataUpdate);
        }

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
            if (!EntityManager.TryGetComponent(uid, out AirAlarmDataComponent data))
                return;

            if (component.HasPlayers())
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

        public void SetThreshold(EntityUid uid, AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null, AirAlarmDataComponent? data = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref data, ref controller)) return;

            _atmosMonitorSystem.SetThreshold(uid, type, threshold, gas);

            controller.SendMessage(new AirAlarmUpdateAlarmThresholdMessage(type, threshold, gas));

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

        public void SetMode(EntityUid uid, AirAlarmMode mode, AirAlarmDataComponent? data = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref data, ref controller)) return;
            controller.SendMessage(new AirAlarmUpdateAlarmModeMessage(mode));
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

        public void SetDeviceData(EntityUid uid, string address, IAtmosDeviceData devData, AirAlarmDataComponent? data = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref data, ref controller)) return;

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
            if (!EntityManager.TryGetComponent<AirAlarmDataComponent>(uid, out var alarmData))
                return;

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

                    controller.SendMessage(new AirAlarmUpdateDeviceDataMessage(args.SenderAddress, data));

                    return;
                case AirAlarmSetDataStatus:
                    if (!args.Data.TryGetValue(AirAlarmSetDataStatus, out bool dataStatus)) break;

                    // Sync data to interface.
                    // This should say if the result
                    // failed, or succeeded. Don't save it.l
                    SyncDevice(uid, args.SenderAddress);

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
        public void SendAirData(EntityUid uid, AirAlarmComponent? alarm = null, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmDataComponent? data = null)
        {
            if (!Resolve(uid, ref alarm, ref monitor, ref power, ref data)) return;

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

                alarm.SendMessage(new AirAlarmUpdateAirDataMessage(airData));
            }

            // alarm.UpdateUI();
        }

        public void SendAlarmMode(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmDataComponent? data = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref data, ref controller)
                || !power.Powered) return;

            controller.SendMessage(new AirAlarmUpdateAlarmModeMessage(data.CurrentMode));

            // _airAlarmDataSystem.UpdateAlarmMode(uid, data.CurrentMode);
            // data.DirtyMode = true;
            // data.Dirty();
        }

        public void SendThresholds(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmDataComponent? data = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref data, ref controller)
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
                controller.SendMessage(new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Pressure, monitor.PressureThreshold));
            }

            if (monitor.TemperatureThreshold != null)
            {
                controller.SendMessage(new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Temperature, monitor.TemperatureThreshold));
                // _airAlarmDataSystem.UpdateAlarmThreshold(uid, monitor.TemperatureThreshold, AtmosMonitorThresholdType.Temperature);
                // data.TemperatureThreshold = monitor.TemperatureThreshold;
                // data.DirtyThresholds.Add(AtmosMonitorThresholdType.Temperature);
            }

            if (monitor.GasThresholds != null)
            {
                // data.DirtyThresholds.Add(AtmosMonitorThresholdType.Gas);
                foreach (var (gas, threshold) in monitor.GasThresholds)
                    controller.SendMessage(new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Gas, threshold, gas));
                    // _airAlarmDataSystem.UpdateAlarmThreshold(uid, threshold, AtmosMonitorThresholdType.Gas, gas);
                    // data.GasThresholds[gas] = threshold;
            }

            data.Dirty();
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
