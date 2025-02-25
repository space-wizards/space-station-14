using Content.Shared.Random;
using Content.Shared.Random.Rules;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Audio;

/// <summary>
/// Attaches a rules prototype to sound files to play ambience.
/// </summary>
[Prototype("ambientMusic")]
public sealed partial class AmbientMusicPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Traditionally you'd prioritise most rules to least as priority but in our case we'll just be explicit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("priority")]
    public int Priority = 0;

    /// <summary>
    /// Can we interrupt this ambience for a better prototype if possible?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("interruptable")]
    public bool Interruptable = false;

    /// <summary>
    /// Do we fade-in. Useful for songs.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fadeIn")]
    public bool FadeIn;

    [ViewVariables(VVAccess.ReadWrite), DataField("sound", required: true)]
    public SoundSpecifier Sound = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("rules", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<RulesPrototype>))]
    public string Rules = string.Empty;
}
