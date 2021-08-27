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
        public IMidiRenderer? Renderer;
        public bool IsRendererAlive => Renderer != null;
    }
}
