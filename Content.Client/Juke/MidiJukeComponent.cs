using Content.Shared.Juke;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Juke
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMidiJukeComponent))]
    public class MidiJukeComponent : SharedMidiJukeComponent
    {
        [Dependency] private readonly IMidiManager _midiManager = default!;
        private IMidiRenderer? _renderer;

        public bool IsRendererAlive => _renderer != null;

        private void SetupRenderer()
        {
            if (IsRendererAlive) return;
            _renderer = _midiManager.GetNewRenderer();

            if (_renderer != null)
            {
                _renderer.DisablePercussionChannel = false;
                _renderer.DisableProgramChangeEvent = false;
                _renderer.TrackingEntity = Owner;
            }
        }

        public void PlayEvents(MidiEvent[] midiEvents)
        {
            if (!IsRendererAlive) SetupRenderer();
            foreach (var evt in midiEvents)
            {
                _renderer?.SendMidiEvent(evt);
            }
        }
    }
}
