using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    [Serializable, NetSerializable]
    public sealed partial class GasVentPumpDataPayload : AtmosDeviceDataPayload
    {
        public VentPumpDirection PumpDirection { get; set; } = VentPumpDirection.Releasing;
        public VentPressureBound PressureChecks { get; set; } = VentPressureBound.ExternalBound;
        public float ExternalPressureBound { get; set; } = Atmospherics.OneAtmosphere;
        public float InternalPressureBound { get; set; } = 0f;
        public bool PressureLockoutOverride { get; set; } = false;

        // Presets for 'dumb' air alarm modes

        public static GasVentPumpDataPayload FilterModePreset = new GasVentPumpDataPayload
        {
            Enabled = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f,
            PressureLockoutOverride = false
        };

        public static GasVentPumpDataPayload FillModePreset = new GasVentPumpDataPayload
        {
            Enabled = true,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere * 50,
            InternalPressureBound = 0f,
            PressureLockoutOverride = true
        };

        public static GasVentPumpDataPayload PanicModePreset = new GasVentPumpDataPayload
        {
            Enabled = false,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f,
            PressureLockoutOverride = false
        };

        public static GasVentPumpDataPayload ReplaceModePreset = new GasVentPumpDataPayload
        {
            Enabled = false,
            IgnoreAlarms = true,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f,
            PressureLockoutOverride = false
        };
    }

    [Serializable, NetSerializable]
    public sealed partial class GasVentPumpSyncDataPayload : HandledNetworkPayload;

    [Serializable, NetSerializable]
    public sealed partial class GasVentPumpSetDataPayload : HandledNetworkPayload
    {
        [DataField]
        public GasVentPumpDataPayload Payload;
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
        Both = 3,
    }
}
