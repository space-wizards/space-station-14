using Robust.Shared.Prototypes;

namespace Content.Server.Radio;

[Prototype("encryptionKeysSet")]
public sealed class EncryptionKeysSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private init; } = default!;
}
