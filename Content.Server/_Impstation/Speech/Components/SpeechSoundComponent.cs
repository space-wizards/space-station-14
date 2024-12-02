using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Speech.Components;

/// <summary>
/// When put on a piece of clothing, modifies the wearer's
/// speech sounds
/// </summary>
[RegisterComponent]
public sealed partial class SpeechSoundComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SpeechSoundsPrototype>? SpeechSounds = null;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SpeechVerbPrototype>? SpeechVerb = null;
}
