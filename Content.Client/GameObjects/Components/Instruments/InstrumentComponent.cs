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
        /// <summary>
        ///     Called when a midi song stops playing.
        /// </summary>
        public event Action OnMidiPlaybackEnded;

#pragma warning disable 649
        [Dependency] private IMidiManager _midiManager;
        [Dependency] private IFileDialogManager _fileDialogManager;
#pragma warning restore 649

        private IMidiRenderer _renderer;
        private int _instrumentProgram = 1;

        /// <summary>
        ///     A queue of MidiEvents to be sent to the server.
        /// </summary>
        private Queue<MidiEvent> _eventQueue = new Queue<MidiEvent>();

        /// <summary>
        ///     Whether a midi song will loop or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool LoopMidi
        {
            get => _renderer.LoopMidi;
            set => _renderer.LoopMidi = value;
        }

        /// <summary>
        ///     Changes the instrument the midi renderer will play.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int InstrumentProgram
        {
            get => _instrumentProgram;
            set {
                _instrumentProgram = value;
                _renderer.MidiProgram = _instrumentProgram;
            }
        }

        /// <summary>
        ///     Whether there's a midi song being played or not.
        /// </summary>
        [ViewVariables]
        public bool IsMidiOpen => _renderer.Status == MidiRendererStatus.File;

        /// <summary>
        ///     Whether the midi renderer is listening for midi input or not.
        /// </summary>
        [ViewVariables]
        public bool IsInputOpen => _renderer.Status == MidiRendererStatus.Input;

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _renderer = _midiManager.GetNewRenderer();
            _renderer.MidiProgram = _instrumentProgram;
            _renderer.Position = Owner;
            _renderer.OnMidiPlayerFinished += () => { OnMidiPlaybackEnded?.Invoke(); };
        }

        protected override void Shutdown()
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
                    // If we're the ones sending the MidiEvents, we ignore this message.
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

        /// <inheritdoc cref="MidiRenderer.OpenInput"/>
        public void OpenInput()
        {
            _renderer.OnMidiEvent += RendererOnMidiEvent;
            _renderer.OpenInput();
        }

        /// <inheritdoc cref="MidiRenderer.CloseInput"/>
        public void CloseInput()
        {
            _renderer.OnMidiEvent -= RendererOnMidiEvent;
            _renderer.CloseInput();
        }

        /// <inheritdoc cref="MidiRenderer.OpenMidi(string)"/>
        public void OpenMidi(string filename)
        {
            _renderer.OnMidiEvent += RendererOnMidiEvent;
            _renderer.OpenMidi(filename);
        }

        /// <inheritdoc cref="MidiRenderer.CloseMidi"/>
        public void CloseMidi()
        {
            _renderer.OnMidiEvent -= RendererOnMidiEvent;
            _renderer.CloseMidi();
        }

        /// <summary>
        ///     Called whenever the renderer receives a midi event.
        /// </summary>
        /// <param name="midiEvent">The received midi event</param>
        private void RendererOnMidiEvent(MidiEvent midiEvent)
        {
            lock (_eventQueue)
                _eventQueue.Enqueue(midiEvent);
        }

        /// <summary>
        ///     Sends queued midi events to the server.
        /// </summary>
        public void Update()
        {
            if (!(IsInputOpen || IsMidiOpen)) return;
            lock (_eventQueue)
            {
                if (!_eventQueue.TryDequeue(out var midiEvent)) return;
                SendNetworkMessage(new InstrumentMidiEventMessage(midiEvent));
            }
        }
    }
}
