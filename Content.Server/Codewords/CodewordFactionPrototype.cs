using Robust.Shared.Prototypes;

namespace Content.Server.Codewords;

/// <summary>
/// This is a prototype for easy access to codewords using identifiers instead of magic strings.
/// </summary>
[Prototype]
public sealed partial class CodewordFactionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The generator to use for this faction.
    /// </summary>
    [DataField(required:true)]
    public ProtoId<CodewordGeneratorPrototype> Generator { get; private set; } = default!;
}
