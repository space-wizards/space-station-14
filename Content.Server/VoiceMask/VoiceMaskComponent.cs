using Content.Shared.Humanoid;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class VoiceMaskComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite)] public string VoiceName = "Unknown";
    [ViewVariables(VVAccess.ReadWrite)] public string VoiceId = SharedHumanoidAppearanceSystem.DefaultVoice; // Corvax-TTS
}
