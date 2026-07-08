using Content.Shared.DeviceNetwork;

namespace Content.Shared.Atmos.Monitor;

public interface IAtmosDeviceData
{
    bool Enabled { get; set; }
    bool Dirty { get; set; }
    bool IgnoreAlarms { get; set; }

    NetworkPayload GetPayload();
}
