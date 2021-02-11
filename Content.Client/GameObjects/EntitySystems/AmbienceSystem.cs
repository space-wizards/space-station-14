#nullable enable
using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AmbienceSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private AudioSystem _audioSystem = default!;

        private SoundCollectionPrototype _ambientCollection = default!;

        private AudioParams _ambientParams = new(-8f, 1, "Master", 0, 0, AudioMixTarget.Stereo, true, 0f);

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystemManager.GetEntitySystem<AudioSystem>();
            _ambientCollection = _prototypeManager.Index<SoundCollectionPrototype>("AmbienceBase");

            var file = _robustRandom.Pick(_ambientCollection.PickFiles);
            _audioSystem.Play(file, _ambientParams);
        }
    }
}

