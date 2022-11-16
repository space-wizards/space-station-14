using Robust.Shared.Prototypes;

namespace Content.Server.Radio;

[Prototype("encryptionKey")]
public sealed class EncryptionKeyPrototype : IPrototype
{
    [IdDataField] public string ID { get; private init; } = default!;
}
