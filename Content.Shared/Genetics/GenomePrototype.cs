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
    [DataField(required: true)]
    public Dictionary<string, ushort> ValueBits = new();
}
