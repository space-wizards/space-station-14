using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Crayon;

[Serializable, NetSerializable]
public sealed partial class FakeConsumableDoAfterEvent : SimpleDoAfterEvent
{
    public FakeConsumableDoAfterEvent()
    {
    }
}
