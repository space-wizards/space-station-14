using Content.Server.BookEncryption;

namespace Content.Server.Construction.Components;

[RegisterComponent, Access(typeof(ForgottenKnowledgeSystem))]
public sealed partial class EncryptedComponent : Component
{
    [DataField]
    public bool Encrypted = true;

    [DataField]
    public List<string> EncryptionKeywords = new ();
}
