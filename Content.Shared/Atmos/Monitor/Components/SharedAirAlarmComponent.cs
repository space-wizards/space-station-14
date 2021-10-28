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
        Filtering,
        Fill,
        Panic,
        Replace,
        None
    }

    public interface IAtmosDeviceData
    {
        public bool Enabled { get; set; }
    }

    [Serializable, NetSerializable]
    public class AirAlarmBoundUserInterfaceState : BoundUserInterfaceState
    {
        public EntityUid Uid { get; }

        public AirAlarmBoundUserInterfaceState(EntityUid uid)
        {
            Uid = uid;
        }
    }

    [Serializable, NetSerializable]
    public class AirAlarmUpdateAlarmModeMessage : BoundUserInterfaceMessage
    {
        public AirAlarmMode Mode { get; }

        public AirAlarmUpdateAlarmModeMessage(AirAlarmMode mode)
        {
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public class AirAlarmUpdateDeviceDataMessage : BoundUserInterfaceMessage
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
    public class AirAlarmUpdateAlarmThresholdMessage : BoundUserInterfaceMessage
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

    [Serializable, NetSerializable]
    public class GasVentPumpData : IAtmosDeviceData
    {
        public bool Enabled { get; set; }
        public VentPumpDirection PumpDirection { get; set; }
        public VentPressureBound PressureChecks { get; set; }
        public float ExternalPressureBound { get; set; }
        public float InternalPressureBound { get; set; }
    }

    [Serializable, NetSerializable]
    public class GasVentScrubberData : IAtmosDeviceData
    {
        public bool Enabled { get; set; }
        public HashSet<Gas> FilterGases { get; set; } = new();
        public ScrubberPumpDirection  PumpDirection { get; set; }
        public float VolumeRate { get; set; }
        public bool WideNet { get; set; }
    }

    [Serializable, NetSerializable]
    public enum ScrubberPumpDirection : sbyte
    {
        Siphoning = 0,
        Scrubbing = 1,
    }

    [Serializable, NetSerializable]
    public enum VentPumpDirection : sbyte
    {
        Siphoning = 0,
        Releasing = 1,
    }

    [Flags]
    [Serializable, NetSerializable]
    public enum VentPressureBound : sbyte
    {
        NoBound       = 0,
        InternalBound = 1,
        ExternalBound = 2,
    }

}
