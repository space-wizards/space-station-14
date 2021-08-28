using System;
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
        public VirtualMidiPlayer? MidiPlayer;
        public string MidiFileName = string.Empty;
        public bool Loop;
    }
}
