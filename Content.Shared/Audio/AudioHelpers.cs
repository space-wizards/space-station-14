using Robust.Shared.Audio;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Audio
{
    public static class AudioHelpers{
        /// <summary>
        ///     Returns a random pitch.
        /// </summary>
        public static AudioParams WithVariation(float amplitude)
        {
            var scale = (float)(IoCManager.Resolve<IRobustRandom>().NextGaussian(1, amplitude));
            return AudioParams.Default.WithPitchScale(scale);
        }

        public static string GetRandomFileFromSoundCollection(string name)
        {
            var soundCollection = IoCManager.Resolve<IPrototypeManager>().Index<SoundCollectionPrototype>(name);
            return IoCManager.Resolve<IRobustRandom>().Pick(soundCollection.PickFiles);
        }
    }
}
