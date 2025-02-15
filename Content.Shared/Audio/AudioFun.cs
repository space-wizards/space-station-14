using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Shared.Audio;

public static class AudioFun
{
    private static readonly RobustRandom _random = new();

    public static AudioParams? FunAudioParams(AudioParams? audioParams)
    {
        if (audioParams == null)
            return null;

        return audioParams.Value.WithPitchScale(_random.NextFloat() * (2.0f - 0.2f) + 0.2f).WithVariation(0);
    }
}
