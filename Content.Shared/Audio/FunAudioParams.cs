using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Shared.Audio;

public static class FunAudioParams
{
    private static readonly RobustRandom Random = new();

    private const float MinPitchScale = 0.1f;
    private const float MaxPitchScale = 2.0f;

    public static AudioParams WithUniformPitch(AudioParams? audioParams = null)
    {
        audioParams ??= AudioParams.Default;

        return audioParams.Value.WithPitchScale(Random.NextFloat() * (MaxPitchScale - MinPitchScale) + MinPitchScale)
            .WithVariation(0);
    }


    public static AudioParams WithUniformPitch()
    {
        return new AudioParams
            { Pitch = Random.NextFloat() * (MaxPitchScale - MinPitchScale) + MinPitchScale, Variation = 0 };
    }
}
