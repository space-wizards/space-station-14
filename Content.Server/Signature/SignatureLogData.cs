using Content.Shared.Signature;
using JetBrains.Annotations;

namespace Content.Server.Signature;

public sealed class SignatureLogData(SignatureData data) : AbstractSignatureLogData(data)
{
    [UsedImplicitly]
    public string Serialized { get; } = SignatureSerializer.Serialize(data);
}
