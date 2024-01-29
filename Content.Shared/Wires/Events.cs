using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Wires;

[Serializable, NetSerializable]
public sealed partial class WireDoAfterEvent : DoAfterEvent
{
    [DataField("action", required: true)]
    public WiresAction Action;

    [DataField("id", required: true)]
    public int Id;

    private WireDoAfterEvent()
    {
    }

    public WireDoAfterEvent(WiresAction action, int id)
    {
        Action = action;
        Id = id;
    }

    public override DoAfterEvent Clone() => this;
}
