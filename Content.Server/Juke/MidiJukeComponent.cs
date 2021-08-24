using System.Collections.Generic;
using Content.Shared.Juke;
using Content.Shared.Stacks;
using Robust.Server.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Juke
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMidiJukeComponent))]
    public class MidiJukeComponent : SharedMidiJukeComponent
    {
        private VirtualMidiPlayer? _midiPlayer;

        private string _midiFileName = "";
        public string MidiFileName
        {
            get => _midiFileName;
            set
            {
                _midiPlayer?.Dispose();
                _midiPlayer = VirtualMidiPlayer.FromFile(value);
                if (_midiPlayer != null) _midiFileName = value;
            }
        }

        public List<MidiEvent> PlayTick()
        {
            return _midiPlayer?.TickClockAndPopEventBuffer() ?? new List<MidiEvent>();
        }

    }
}
