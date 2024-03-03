using Content.Shared.DeviceLinking;
using Content.Shared.Librarian;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Librarian.Components;

/// <summary>
/// A component storing randomized pairs to keywords.
/// </summary>
[RegisterComponent, Access(typeof(BooksEncryptionSystem))]
public sealed partial class BooksEncryptionComponent : Component
{
    [DataField]
    public Dictionary<EncryptedBookDisciplinePrototype,Dictionary<string, string>> KeywordPairs = new();
}
