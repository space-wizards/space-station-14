using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Carrying.Events;

/// <summary>
/// DoAfter event for the carry windup.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CarryDoAfterEvent : SimpleDoAfterEvent;
