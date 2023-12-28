using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Emag.Events;

[Serializable, NetSerializable]
public sealed partial class DeemagDoAfterEvent : SimpleDoAfterEvent
{
}
