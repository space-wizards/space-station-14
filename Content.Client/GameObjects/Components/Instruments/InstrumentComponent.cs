using System;
using System.IO;
using System.Linq;
using Content.Shared.GameObjects.Components.Instruments;
using Robust.Shared.GameObjects;
using Robust.Client.Audio.Midi;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;


namespace Content.Client.GameObjects.Components.Instruments
{
    [RegisterComponent]
    public class InstrumentComponent : SharedInstrumentComponent
    {
#pragma warning disable 649
        [Dependency] private IMidiManager _midiManager;
        [Dependency] private IFileDialogManager _fileDialogManager;
#pragma warning restore 649

        private IMidiRenderer _renderer;
        private int _instrumentProgram = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        public int InstrumentProgram
        {
            get => _instrumentProgram;
            set {
                _instrumentProgram = value;
                _renderer.MidiProgram = _instrumentProgram;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _renderer = _midiManager.GetNewRenderer();
            _renderer.LoadSoundfont("soundfont.sf2");
            _renderer.MidiProgram = _instrumentProgram;
            _renderer.Position = Owner;
            _renderer.OpenInput();
            //_renderer.OpenMidi("mysong.mid");
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _renderer?.Dispose();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _instrumentProgram, "program", 1);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMessage:
                    if (_renderer.IsInputOpen || _renderer.IsMidiOpen) break;
                    _renderer.SendMidiEvent(midiEventMessage.MidiEvent);
                    break;
            }
        }
    }
}
