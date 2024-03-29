using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Paint;

[Serializable, NetSerializable]
public sealed partial class PaintDoAfterEvent : SimpleDoAfterEvent
{
}
