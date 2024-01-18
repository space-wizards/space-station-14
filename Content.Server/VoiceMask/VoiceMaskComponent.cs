using Content.Shared.Speech;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class VoiceMaskComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite)] public string VoiceName = "Unknown";

    [ViewVariables(VVAccess.ReadWrite)] public SpeechVerbPrototype? SpeechVerb;
}
