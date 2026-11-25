using Robust.Shared.Prototypes;

namespace Content.Server.Codewords;

/// <summary>
/// Component that defines <see cref="CodewordGeneratorPrototype"/> to use and keeps track of generated codewords.
/// </summary>
[RegisterComponent, Access(typeof(CodewordSystem))]
public sealed partial class CodewordManagerComponent : Component
{
    /// <summary>
    /// The generated codewords. The value contains the entity that has the <see cref="CodewordComponent"/>
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<CodewordFactionPrototype>, EntityUid> Codewords = new();
}
