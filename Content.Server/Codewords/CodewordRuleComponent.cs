using Robust.Shared.Prototypes;

namespace Content.Server.Codewords;

/// <summary>
/// Component that defines <see cref="CodewordGenerator"/> to use and keeps track of generated codewords.
/// </summary>
[RegisterComponent, Access(typeof(CodewordSystem))]
public sealed partial class CodewordRuleComponent : Component
{
    /// <summary>
    /// The generators available.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<CodewordFaction>, ProtoId<CodewordGenerator>> Generators = new();

    /// <summary>
    /// The generated codewords. The value contains the entity that has the <see cref="CodewordComponent"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<ProtoId<CodewordFaction>, EntityUid> Codewords = new();
}
