using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.Codewords;

/// <summary>
/// This is a prototype for specifying codeword generation
/// </summary>
[Prototype]
public sealed partial class CodewordGeneratorPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// List of datasets to use for word generation. All values will be concatenated into one list and then randomly chosen from
    /// </summary>
    [DataField]
    public List<ProtoId<LocalizedDatasetPrototype>> Words { get; } =
    [
        "Adjectives",
        "Verbs",
    ];


    /// <summary>
    /// How many codewords should be generated?
    /// </summary>
    [DataField]
    public int Amount = 3;
}
