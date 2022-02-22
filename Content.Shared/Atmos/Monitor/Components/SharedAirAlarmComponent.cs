using System;
using System.Collections.Generic;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor.Components
{
    [Serializable, NetSerializable]
    public enum SharedAirAlarmInterfaceKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum AirAlarmMode
    {
        None,
        Filtering,
        Fill,
        Panic,
        Replace
    }

    [Serializable, NetSerializable]
    public enum AirAlarmWireStatus
    {
        Power,
        Access,
        Panic,
        DeviceSync
    }

    [Serializable, NetSerializable]
    public readonly struct AirAlarmAirData
    {
        public readonly float? Pressure { get; }
        public readonly float? Temperature { get; }
        public readonly float? TotalMoles { get; }
        public readonly AtmosMonitorAlarmType AlarmState { get; }

        private readonly Dictionary<Gas, float>? _gases;
        public readonly IReadOnlyDictionary<Gas, float>? Gases { get => _gases; }

        public AirAlarmAirData(float? pressure, float? temperature, float? moles, AtmosMonitorAlarmType state, Dictionary<Gas, float>? gases)
        {
            Pressure = pressure;
            Temperature = temperature;
            TotalMoles = moles;
            AlarmState = state;
            _gases = gases;
        }
    }

    public interface IAtmosDeviceData
    {
        public bool Enabled { get; set; }
        public bool Dirty { get; set; }
        public bool IgnoreAlarms { get; set; }
    }

    // would be nice to include the entire state here
    // but it's already handled by messages
    [Serializable, NetSerializable]
    public sealed class AirAlarmUIState : BoundUserInterfaceState
    {}

    [Serializable, NetSerializable]
    public sealed class AirAlarmResyncAllDevicesMessage : BoundUserInterfaceMessage
    {}

    [Serializable, NetSerializable]
    public sealed class AirAlarmSetAddressMessage : BoundUserInterfaceMessage
    {
        public string Address { get; }

        public AirAlarmSetAddressMessage(string address)
        {
            Address = address;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AirAlarmUpdateAirDataMessage : BoundUserInterfaceMessage
    {
        public AirAlarmAirData AirData;

        public AirAlarmUpdateAirDataMessage(AirAlarmAirData airData)
        {
            AirData = airData;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AirAlarmUpdateAlarmModeMessage : BoundUserInterfaceMessage
    {
        public AirAlarmMode Mode { get; }

        public AirAlarmUpdateAlarmModeMessage(AirAlarmMode mode)
        {
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AirAlarmUpdateDeviceDataMessage : BoundUserInterfaceMessage
    {
        public string Address { get; }
        public IAtmosDeviceData Data { get; }

        public AirAlarmUpdateDeviceDataMessage(string addr, IAtmosDeviceData data)
        {
            Address = addr;
            Data = data;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AirAlarmUpdateAlarmThresholdMessage : BoundUserInterfaceMessage
    {
        public AtmosAlarmThreshold Threshold { get; }
        public AtmosMonitorThresholdType Type { get; }
        public Gas? Gas { get; }

        public AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType type, AtmosAlarmThreshold threshold, Gas? gas = null)
        {
            Threshold = threshold;
            Type = type;
            Gas = gas;
        }
    }


}
