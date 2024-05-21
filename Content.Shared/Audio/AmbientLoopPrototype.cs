using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Audio;

/// <summary>
/// Attaches a rules prototype to sound files to play ambience. Allows you to play cyclic ambient background sound.
/// </summary>
[Prototype("ambientLoop")]
public sealed partial class AmbientLoopPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    /// <summary>
    /// Traditionally you'd prioritise most rules to least as priority but in our case we'll just be explicit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int Priority = 0;

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public ProtoId<RulesPrototype> Rules = string.Empty;
}
