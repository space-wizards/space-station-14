using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.NameIdentifier;

[Prototype]
public sealed partial class NameIdentifierGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Should the identifier become the full name, or just append?
    /// </summary>
    [DataField]
    public bool FullName = false;

    /// <summary>
    /// Should the identifier be a prefix, or a suffix?
    /// </summary>
    [DataField]
    public bool Prefix = false;

    /// <summary>
    /// Optional format identifier. If set, the name will be formatted using it (e.g., "MK-500").
    /// If not set, only the numeric part will be used (e.g., "500").
    /// </summary>
    [DataField]
    public LocId? Format;

    /// <summary>
    /// The maximal value appearing in an identifier.
    /// </summary>
    [DataField]
    public int MaxValue = 1000;

    /// <summary>
    /// The minimal value appearing in an identifier.
    /// </summary>
    [DataField]
    public int MinValue = 0;

    /// <summary>
    /// An optional field that will provide a list of name elements to pull from.
    /// If non-null, <see cref="MinValue"/> and <see cref="MaxValue"/> will be ignored.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? IdentifierDataset;
}
