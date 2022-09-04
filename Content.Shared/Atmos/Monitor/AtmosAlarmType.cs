using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

[Serializable, NetSerializable]
public enum AtmosAlarmType : sbyte
{
    Invalid = 0,
    Normal = 1,
    Warning = 2,
    Danger = 3, // 1 << 1 is the exact same thing and we're not really doing **bitmasking** are we?
    Emagged = 4,
}
