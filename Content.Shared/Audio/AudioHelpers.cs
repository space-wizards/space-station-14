using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Audio
{
    public static class AudioHelpers
    {
        /// <summary>
        ///     Returns a random pitch.
        /// </summary>
        [Obsolete("Use AudioParams.Variation data-field")]
        public static AudioParams WithVariation(float amplitude)
        {
            return WithVariation(amplitude, null);
        }

        /// <summary>
        ///     Returns a random pitch.
        /// </summary>
        [Obsolete("Use AudioParams.Variation data-field")]
        public static AudioParams WithVariation(float amplitude, IRobustRandom? rand)
        {
            IoCManager.Resolve(ref rand);
            var scale = (float) rand.NextGaussian(1, amplitude);
            return AudioParams.Default.WithPitchScale(scale);
        }

        // Might as well just hardcode these because the audio system is limited to pitching up and down
        // by 12 semitones anyway (ie. 0.5 to 2.0 multiplier).
        private static readonly float[] SemitoneMultipliers =
        {
            0.5f, 233.08f/440f, 246.94f/440f, 261.63f/440f,
            277.18f/440f, 293.66f/440f, 311.13f/440f, 329.63f/440f,
            349.23f/440f, 369.99f/440f, 392.00f/440f, 415.30f/440f,
            1.0f,
            466.16f/440f, 493.88f/440f, 523.25f/440f, 554.37f/440f,
            587.33f/440f, 622.25f/440f, 659.26f/440f, 698.46f/440f,
            739.99f/440f, 783.99f/440f, 830.61f/440f, 2.0f
        };

        /// <summary>
        /// Returns a pitch multiplier that shifts by the given number of semitones.
        /// </summary>
        /// <param name="shift">Number of semitones to shift, positive or negative. Clamped between -12 and 12
        /// which correspond to a pitch multiplier of 0.5 and 2.0 respectively.</param>
        public static AudioParams ShiftSemitone(AudioParams @params, int shift)
        {
            shift = MathHelper.Clamp(shift, -12, 12);
            float pitchMult = SemitoneMultipliers[shift + 12];
            return @params.WithPitchScale(pitchMult);
        }

        /// <summary>
        /// Returns a pitch multiplier shifted by a random number of semitones within variation.
        /// </summary>
        /// <param name="variation">Max number of semitones to shift in either direction. Values above 12 have no effect.</param>
        public static AudioParams WithSemitoneVariation(AudioParams @params, int variation, IRobustRandom rand)
        {
            IoCManager.Resolve(ref rand);
            variation = Math.Clamp(variation, 0, 12);
            return ShiftSemitone(@params, rand.Next(-variation, variation));
        }
    }
}
