using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Shared.GameObjects.Components.Instruments;
using OpenTK.Platform.Windows;
using Robust.Shared.GameObjects;
using Robust.Client.Audio.Midi;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Reflection;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
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
        private Queue<MidiEvent> _eventQueue = new Queue<MidiEvent>();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool LoopMidi
        {
            get => _renderer.LoopMidi;
            set => _renderer.LoopMidi = value;
        }

        public event Action OnMidiPlaybackEnded;

        [ViewVariables(VVAccess.ReadWrite)]
        public int InstrumentProgram
        {
            get => _instrumentProgram;
            set {
                _instrumentProgram = value;
                _renderer.MidiProgram = _instrumentProgram;
            }
        }

        [ViewVariables]
        public bool IsMidiOpen => _renderer.IsMidiOpen;

        [ViewVariables]
        public bool IsInputOpen => _renderer.IsInputOpen;

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _renderer = _midiManager.GetNewRenderer();
            _renderer.LoadSoundfont("soundfont.sf2");
            _renderer.MidiProgram = _instrumentProgram;
            _renderer.Position = Owner;
            _renderer.OnMidiPlayerFinished += () => { OnMidiPlaybackEnded?.Invoke(); };
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
                    if (IsInputOpen || IsMidiOpen) break;
                    _renderer.SendMidiEvent(midiEventMessage.MidiEvent);
                    break;

                case InstrumentStopMidiMessage _:
                    _renderer.StopAllNotes();
                    if(IsInputOpen) CloseInput();
                    if(IsMidiOpen) CloseMidi();
                    break;
            }
        }

        public void OpenInput()
        {
            _renderer.OnMidiEvent += RendererOnMidiEvent;
            _renderer.OpenInput();
        }

        public void CloseInput()
        {
            _renderer.OnMidiEvent -= RendererOnMidiEvent;
            _renderer.CloseInput();
        }

        public void OpenMidi(string filename)
        {
            _renderer.OnMidiEvent += RendererOnMidiEvent;
            _renderer.OpenMidi(filename);
        }

        public void CloseMidi()
        {
            _renderer.OnMidiEvent -= RendererOnMidiEvent;
            _renderer.CloseMidi();
        }

        private void RendererOnMidiEvent(MidiEvent obj)
        {
            lock (_eventQueue)
                _eventQueue.Enqueue(obj);
        }

        public void Update()
        {
            lock (_eventQueue)
            {
                if (!_eventQueue.TryDequeue(out var midiEvent)) return;
                SendNetworkMessage(new InstrumentMidiEventMessage(midiEvent));
            }
        }
    }
}
