using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Resist.Events;

[Serializable, NetSerializable]
public sealed partial class EscapeInventoryEvent : SimpleDoAfterEvent;
