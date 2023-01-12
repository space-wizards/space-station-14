using Content.Server.Corvax.TTS;
using Content.Shared.Humanoid;

namespace Content.Server.Humanoid;

public sealed partial class HumanoidSystem
{
    // ReSharper disable once InconsistentNaming
    public void SetTTSVoice(EntityUid uid, string voiceId, HumanoidComponent humanoid)
    {
        if (!TryComp<TTSComponent>(uid, out var comp))
            return;

        humanoid.Voice = voiceId;
        comp.VoicePrototypeId = voiceId;
    }
}
