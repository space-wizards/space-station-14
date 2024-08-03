using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

[Serializable, NetSerializable]
public sealed partial class VacuumCleanerDoAfterEvent : SimpleDoAfterEvent
{
}
