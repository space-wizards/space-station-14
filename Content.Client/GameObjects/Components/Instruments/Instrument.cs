using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Client.Audio.Midi;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.Instruments
{
    [RegisterComponent]
    public class Instrument : Component
    {
        public override string Name => "Instrument";

        [Dependency] private IMidiManager _midiManager;
        [Dependency] private IClydeAudio _clydeAudio;
        [Dependency] private IEntitySystemManager _entitySystemManager;
        private AudioSystem _audioSystem;
        private IMidiRenderer _renderer;

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _renderer = _midiManager.GetNewRenderer();
            _renderer.OnSampleRendered += RendererOnOnSampleRendered;
            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            _renderer.LoadSoundfont("soundfont.sf2");
            _renderer.MidiProgram = 1;
            _renderer.OpenInput(_midiManager.Inputs.Last().Id);
            //_renderer.OpenMidi(File.Open("mysong.mid", FileMode.Open));
        }

        private void RendererOnOnSampleRendered((ushort[] left, ushort[] right) obj)
        {
            var left = _clydeAudio.LoadAudioMonoPCM(obj.left);
            var right = _clydeAudio.LoadAudioMonoPCM(obj.right);
            _audioSystem.Play(left, Owner.Transform.GridPosition);
            _audioSystem.Play(right, Owner.Transform.GridPosition);
        }
    }
}
