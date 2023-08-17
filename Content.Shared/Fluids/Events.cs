using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids;

[Serializable, NetSerializable]
public sealed partial class AbsorbantDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public string TargetSolution { get; private set; } = default!;

    [DataField("message", required: true)]
    public string Message { get; private set; } = default!;

    [DataField("sound", required: true)]
    public SoundSpecifier Sound { get; private set; } = default!;

    [DataField("transferAmount", required: true)]
    public FixedPoint2 TransferAmount { get; private set; }

    private AbsorbantDoAfterEvent()
    {
    }

    public AbsorbantDoAfterEvent(string targetSolution, string message, SoundSpecifier sound, FixedPoint2 transferAmount)
    {
        TargetSolution = targetSolution;
        Message = message;
        Sound = sound;
        TransferAmount = transferAmount;
    }

    public override DoAfterEvent Clone() => this;
}
