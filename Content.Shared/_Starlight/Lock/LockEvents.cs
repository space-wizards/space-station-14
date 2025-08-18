using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Lock;

[Serializable, NetSerializable]
public sealed partial class DigitalLockMaintenanceOpenDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class DigitalLockResetDoAfterEvent : SimpleDoAfterEvent
{
}