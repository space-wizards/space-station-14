using Robust.Shared.Prototypes;

namespace Content.Server.Codewords;

/// <summary>
/// Component that defines <see cref="CodewordGeneratorPrototype"/> to use and keeps track of generated codewords.
/// </summary>
[RegisterComponent, Access(typeof(CodewordSystem))]
public sealed partial class CodewordRuleComponent : Component
{
    /// <summary>
    /// The generators available.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<CodewordFactionPrototype>, ProtoId<CodewordGeneratorPrototype>> Generators = new();

    /// <summary>
    /// The generated codewords. The value contains the entity that has the <see cref="CodewordComponent"/>
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<ProtoId<CodewordFactionPrototype>, EntityUid> Codewords = new();
}
