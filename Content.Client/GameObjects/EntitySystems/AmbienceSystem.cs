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
using Robust.Client;
using Robust.Client.State;
using Content.Client.State;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AmbienceSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;

        private AudioSystem _audioSystem = default!;

        private SoundCollectionPrototype _ambientCollection = default!;

        private AudioParams _ambientParams = new(-10f, 1, "Master", 0, 0, AudioMixTarget.Stereo, true, 0f);

        private IPlayingAudioStream? _ambientStream;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystemManager.GetEntitySystem<AudioSystem>();
            _ambientCollection = _prototypeManager.Index<SoundCollectionPrototype>("AmbienceBase");

            _configManager.OnValueChanged(CCVars.AmbienceBasicEnabled, AmbienceCVarChanged);

            _stateManager.OnStateChanged += StateManagerOnStateChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _stateManager.OnStateChanged -= StateManagerOnStateChanged;
        }

        private void StateManagerOnStateChanged(StateChangedEventArgs args)
        {
            if (args.NewState is not GameScreen)
            {
                EndAmbience();
            }
            else if (_configManager.GetCVar(CCVars.AmbienceBasicEnabled))
            {
                StartAmbience();
            }
        }

        private void AmbienceCVarChanged(bool ambienceEnabled)
        {
            if (!ambienceEnabled)
            {
                EndAmbience();
            }
            else if (_stateManager.CurrentState is GameScreen)
            {
                StartAmbience();
            }
        }

        private void StartAmbience()
        {
            EndAmbience();
            var file = _robustRandom.Pick(_ambientCollection.PickFiles);
            _ambientStream = _audioSystem.Play(file, _ambientParams);
        }

        private void EndAmbience()
        {
            if (_ambientStream == null)
            {
                return;
            }
            _ambientStream.Stop();
            _ambientStream = null;
        }
    }
}

