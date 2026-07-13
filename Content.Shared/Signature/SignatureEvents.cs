using Robust.Shared.Serialization;

namespace Content.Shared.Signature;

[Serializable, NetSerializable]
public sealed class SignatureSubmitMessage(SignatureData data) : BoundUserInterfaceMessage
{
    public SignatureData Data = data;
}

[Serializable, NetSerializable]
public sealed class ApplySavedSignature : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class UpdateSignatureDataState(SignatureData data) : BoundUserInterfaceState
{
    public SignatureData Data = data;
}

[Serializable, NetSerializable]
public sealed class UpdatePenBrushPaperState(int brushWriteSize, int brushEraseSize) : BoundUserInterfaceState
{
    public int BrushWriteSize = brushWriteSize;
    public int BrushEraseSize = brushEraseSize;
}

[Serializable, NetSerializable]
public sealed class RequestSignatureAdminMessage(int logId, DateTime time) : BoundUserInterfaceMessage
{
    public int LogId = logId;
    public DateTime Time = time;
}

[Serializable, NetSerializable]
public sealed class SendSignatureToAdminEvent(SignatureData data) : EntityEventArgs
{
    public SignatureData Data = data;
}
