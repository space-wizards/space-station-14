using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Lock;

[Serializable, NetSerializable]
public sealed partial class DigitalLockMaintenanceOpenDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class DigitalLockResetDoAfterEvent : SimpleDoAfterEvent
{
}

[NetSerializable]
[Serializable]
public enum DigitalLockVisuals : byte
{
    Spark
}