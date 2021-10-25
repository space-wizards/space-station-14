using System;
using System.Collections.Generic;
using Content.Shared.Atmos;
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
        public float? Pressure { get; set; }
        public float? Temperature { get; set; }
        public Dictionary<Gas, float> Gases { get; set; } = new();
        public float? TotalMoles { get; set; }
        public Dictionary<string, IAtmosDeviceData> DeviceData { get; set; } = new();
        public AirAlarmMode CurrentMode { get; set; }
    }

    [Serializable, NetSerializable]
    public class AirAlarmChangeModeMessage : BoundUserInterfaceMessage
    {
        public AirAlarmMode Mode { get; }

        public AirAlarmChangeModeMessage(AirAlarmMode mode)
        {
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public class AirAlarmChangeDeviceDataMessage : BoundUserInterfaceMessage
    {
        public string Address { get; }
        public IAtmosDeviceData Data { get; }

        public AirAlarmChangeDeviceDataMessage(string address, IAtmosDeviceData data)
        {
            Address = address;
            Data = data;
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
