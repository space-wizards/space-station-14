#nullable enable
using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
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
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private AudioSystem _audioSystem = default!;

        private SoundCollectionPrototype _ambientCollection = default!;

        private AudioParams _ambientParams = new(-10f, 1, "Master", 0, 0, AudioMixTarget.Stereo, true, 0f);

        private IPlayingAudioStream? _ambientStream;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystemManager.GetEntitySystem<AudioSystem>();
            _ambientCollection = _prototypeManager.Index<SoundCollectionPrototype>("AmbienceBase");
            _configManager.OnValueChanged(CCVars.AmbienceBasicEnabled, HandleAmbience, true);
        }

        private void HandleAmbience(bool ambienceEnabled)
        {
            if (ambienceEnabled)
            {
                var file = _robustRandom.Pick(_ambientCollection.PickFiles);
                _ambientStream = _audioSystem.Play(file, _ambientParams);
            }
            else if (_ambientStream != null)
            {
                _ambientStream.Stop();
                _ambientStream = null;
            }
        }
    }
}

