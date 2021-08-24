using Content.Shared.Juke;
using Robust.Shared.GameObjects;

namespace Content.Client.Juke
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMidiJukeComponent))]
    public class MidiJukeComponent : SharedMidiJukeComponent
    {

    }
}
