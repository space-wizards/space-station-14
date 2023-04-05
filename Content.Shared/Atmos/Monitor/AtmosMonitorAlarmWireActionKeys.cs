using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

[Serializable, NetSerializable]
public enum AtmosMonitorAlarmWireActionKeys : byte
{
    Network,
}
