using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._MACRO.Speech.Components;

/// <summary>
/// When put on a piece of clothing, modifies the wearer's
/// speech sounds
/// </summary>
[RegisterComponent]
public sealed partial class SpeechSoundComponent : Component
{
    /// <summary>
    /// When given, replace speech sounds with the provided prototype.
    /// </summary>
    [DataField]
    public ProtoId<SpeechSoundsPrototype>? SpeechSounds;

    /// <summary>
    /// When given, replace speech verbs with the provided prototype.
    /// </summary>
    [DataField]
    public ProtoId<SpeechVerbPrototype>? SpeechVerb;
}
