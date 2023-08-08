using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids;

[Serializable, NetSerializable]
public sealed class AbsorbantDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public readonly string TargetSolution = default!;

    [DataField("message", required: true)]
    public readonly string Message = default!;

    [DataField("sound", required: true)]
    public readonly SoundSpecifier Sound = default!;

    [DataField("transferAmount", required: true)]
    public readonly FixedPoint2 TransferAmount;

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
