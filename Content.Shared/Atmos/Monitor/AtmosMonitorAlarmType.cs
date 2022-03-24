using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor
{
    [Serializable, NetSerializable]
    public enum AtmosMonitorAlarmType : sbyte
    {
        Normal = 0,
        Warning = 1,
        Danger = 2, // 1 << 1 is the exact same thing and we're not really doing **bitmasking** are we?
        Emagged = 3,
    }
}
