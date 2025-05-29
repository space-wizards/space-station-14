using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.Codewords;

/// <summary>
/// This is a prototype for specifying codeword generation
/// </summary>
[Prototype("codewordGenerator")]
public sealed partial class CodewordGenerator : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The adjectives to be used by the generator
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CodewordAdjectives = "Adjectives";

    /// <summary>
    /// The verbs to be used by the generator
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CodewordVerbs = "Verbs";

    /// <summary>
    /// How many codewords should be generated?
    /// </summary>
    [DataField]
    public int Amount = 3;
}
