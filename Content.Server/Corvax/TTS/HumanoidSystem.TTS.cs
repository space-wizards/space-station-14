using Content.Server.Corvax.TTS;
using Content.Shared.Humanoid;

namespace Content.Server.Humanoid;

public sealed partial class HumanoidSystem
{
    // ReSharper disable once InconsistentNaming
    public void SetTTSVoice(EntityUid uid, Sex sex, string voiceId)
    {
        if (!TryComp<TTSComponent>(uid, out var comp))
            return;

        comp.VoicePrototypeId = voiceId;
    }
}
