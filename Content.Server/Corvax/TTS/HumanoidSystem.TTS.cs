using Content.Server.Corvax.TTS;
using Content.Shared.Humanoid;

namespace Content.Server.Humanoid;

public sealed partial class HumanoidAppearanceSystem
{
    // ReSharper disable once InconsistentNaming
    public void SetTTSVoice(EntityUid uid, string voiceId, HumanoidAppearanceComponent humanoid)
    {
        if (!TryComp<TTSComponent>(uid, out var comp))
            return;

        humanoid.Voice = voiceId;
        comp.VoicePrototypeId = voiceId;
    }
}
