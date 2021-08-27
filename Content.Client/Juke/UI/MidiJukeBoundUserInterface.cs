using Content.Client.Juke.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Juke.UI
{
    public class MidiJukeBoundUserInterface : BoundUserInterface
    {
        private MidiJukeMenu? _midiJukeMenu;

        public MidiJukeComponent? MidiJuke { get; set; }

        public MidiJukeBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {

        }

        protected override void Dispose(bool disposing)
        {

        }
    }
}
