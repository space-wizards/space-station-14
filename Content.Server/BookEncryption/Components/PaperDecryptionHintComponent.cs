using Content.Shared.DeviceLinking;
using Content.Shared.Librarian;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.BookEncryption.Components;

/// <summary>
/// A component storing randomized pairs to keywords.
/// </summary>
[RegisterComponent, Access(typeof(BooksEncryptionSystem))]
public sealed partial class PaperDecryptionHintComponent : Component
{
    [DataField(required: true)]
    public ProtoId<EncryptedBookDisciplinePrototype> Discipline;

    [DataField]
    public int MinHints = 1;

    public int MaxHints = 3;
}
