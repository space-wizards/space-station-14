using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components;

[Serializable, NetSerializable]
public sealed partial class GasVentScrubberData : BaseAtmosDeviceData
{
    public override void RaisePayload(EntityUid uid, string address, DeviceNetworkSystem deviceNetSys)
    {
        var payload = new GasVentScrubberSetDataPayload { Data = this };
        deviceNetSys.QueuePacket(uid, address, ref payload);
    }

    public HashSet<Gas> FilterGases { get; set; } = new(DefaultFilterGases);
    public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;
    public float VolumeRate { get; set; } = 200f;
    public bool WideNet { get; set; } = false;
    public bool AirAlarmPanicWireCut { get; set; }

    public static HashSet<Gas> DefaultFilterGases = new()
    {
        Gas.CarbonDioxide,
        Gas.Plasma,
        Gas.Tritium,
        Gas.WaterVapor,
        Gas.Ammonia,
        Gas.NitrousOxide,
        Gas.Frezon
    };

    // Presets for 'dumb' air alarm modes

    public static GasVentScrubberData FilterModePreset = new GasVentScrubberData
    {
        Enabled = true,
        FilterGases = new(GasVentScrubberData.DefaultFilterGases),
        PumpDirection = ScrubberPumpDirection.Scrubbing,
        VolumeRate = 200f,
        WideNet = false
    };

    public static GasVentScrubberData WideFilterModePreset = new GasVentScrubberData
    {
        Enabled = true,
        FilterGases = new(GasVentScrubberData.DefaultFilterGases),
        PumpDirection = ScrubberPumpDirection.Scrubbing,
        VolumeRate = 200f,
        WideNet = true
    };

    public static GasVentScrubberData FillModePreset = new GasVentScrubberData
    {
        Enabled = false,
        Dirty = true,
        FilterGases = new(GasVentScrubberData.DefaultFilterGases),
        PumpDirection = ScrubberPumpDirection.Scrubbing,
        VolumeRate = 200f,
        WideNet = false
    };

    public static GasVentScrubberData PanicModePreset = new GasVentScrubberData
    {
        Enabled = true,
        Dirty = true,
        FilterGases = new(GasVentScrubberData.DefaultFilterGases),
        PumpDirection = ScrubberPumpDirection.Siphoning,
        VolumeRate = 200f,
        WideNet = true
    };

    public static GasVentScrubberData ReplaceModePreset = new GasVentScrubberData
    {
        Enabled = true,
        IgnoreAlarms = true,
        Dirty = true,
        FilterGases = new(GasVentScrubberData.DefaultFilterGases),
        PumpDirection = ScrubberPumpDirection.Siphoning,
        VolumeRate = 200f,
        WideNet = false
    };
}

/// <summary>
/// Used to set <see cref="GasVentScrubberData"/>.
/// </summary>
public partial record struct GasVentScrubberSetDataPayload : INetworkPayload
{
    [DataField]
    public GasVentScrubberData Data;
}

[Serializable, NetSerializable]
public enum ScrubberPumpDirection : sbyte
{
    Siphoning = 0,
    Scrubbing = 1,
}
