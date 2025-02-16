using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics;

/// <summary>
/// Stores names of every bool and int field on an organism's <see cref="Genome"/>.
/// Each genome prototype has a <see cref="GenomeLayout"/> generated roundstart and stored in <see cref="GenomeSystem"/>.
/// </summary>
[Prototype("genome")]
public sealed class GenomePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Names and bit lengths of each value in the genome.
    /// If length is 1 then it will be a bool.
    /// </summary>
    [DataField]
    public Dictionary<string, ushort> ValueBits = new();

    /// <summary>
    /// Values in the genome that store a prototype id of a certain type.
    /// The prototype name must be present in <see cref="PrototypeWhitelist"/>.
    /// The number of bits used is the minimum needed to allow every id in the whitelist.
    /// </summary>
    [DataField]
    public Dictionary<string, string> Prototypes = new();

    /// <summary>
    /// For each prototype, the valid prototype ids that can be indexed by gene values.
    /// This lets genomes store prototype ids as bits instead of somehow storing strings.
    /// </summary>
    [DataField]
    public Dictionary<string, List<string>> PrototypeIds = new();
}
