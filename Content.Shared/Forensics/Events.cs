using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Forensics;

[Serializable, NetSerializable]
public sealed class ForensicScannerDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class ForensicPadDoAfterEvent : DoAfterEvent
{
    [DataField("sample", required: true)] public readonly string Sample = default!;

    private ForensicPadDoAfterEvent()
    {
    }

    public ForensicPadDoAfterEvent(string sample)
    {
        Sample = sample;
    }

    public override DoAfterEvent Clone() => this;
}
