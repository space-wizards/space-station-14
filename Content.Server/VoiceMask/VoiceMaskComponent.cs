using Content.Shared.Speech;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class VoiceMaskComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite)] public string VoiceName = "Unknown";

    /// <summary>
    /// If EnableSpeechVerbModification is true, overrides the speech verb used when this entity speaks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [ValidatePrototypeId<SpeechVerbPrototype>]
    public string SpeechVerb = "Default";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool EnableSpeechVerbModification = false;
}
