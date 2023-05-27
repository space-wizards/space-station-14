using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Audio;

/// <summary>
/// Attaches a rules prototype to sound files to play ambience.
/// </summary>
[Prototype("ambientMusic")]
public sealed class AmbientMusicPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    /// <summary>
    /// Traditionally you'd prioritise most rules to least as priority but in our case we'll just be explicit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("priority")]
    public int Priority = 0;

    [ViewVariables(VVAccess.ReadWrite), DataField("sound", required: true)]
    public SoundSpecifier Sound = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("rules", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<RulesPrototype>))]
    public string Rules = string.Empty;
}
