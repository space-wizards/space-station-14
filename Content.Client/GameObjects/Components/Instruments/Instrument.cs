using System;
using System.IO;
using System.Linq;
using Robust.Shared.GameObjects;
using Commons.Music.Midi;
using NFluidsynth;
using Robust.Client.Audio.Midi;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Logger = NFluidsynth.Logger;
using MidiEvent = NFluidsynth.MidiEvent;

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
            Robust.Shared.Log.Logger.Info("fuck");
            var left = _clydeAudio.LoadAudioMonoPCM(obj.left);
            var right = _clydeAudio.LoadAudioMonoPCM(obj.right);
            _audioSystem.Play(left, Owner.Transform.GridPosition);
            _audioSystem.Play(right, Owner.Transform.GridPosition);
        }
    }
}
