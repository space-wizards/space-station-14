using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Forensics.Events;

[Serializable, NetSerializable]
public sealed partial class ForensicPadDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public string Sample = default!;

    private ForensicPadDoAfterEvent()
    {
    }

    public ForensicPadDoAfterEvent(string sample)
    {
        Sample = sample;
    }

    public override DoAfterEvent Clone() => this;
}
