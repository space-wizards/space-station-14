using Robust.Shared.Prototypes;

namespace Content.Shared.Librarian;

[Prototype("librarianBookDiscipline")]
public sealed partial class LibrarianBookDisciplinePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name = string.Empty;
    [DataField] public Color Color = Color.White;
    [DataField] public List<LocId> Keywords = new List<LocId>();
    [DataField] public List<LocId> Gibberish = new List<LocId>();
}
