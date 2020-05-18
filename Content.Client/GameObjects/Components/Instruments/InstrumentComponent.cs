using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Instruments;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;


namespace Content.Client.GameObjects.Components.Instruments
{
    [RegisterComponent]
    public class InstrumentComponent : SharedInstrumentComponent
    {
        public const float TimeBetweenNetMessages = 1f;

        /// <summary>
        ///     Called when a midi song stops playing.
        /// </summary>
        public event Action OnMidiPlaybackEnded;

#pragma warning disable 649
        [Dependency] private IMidiManager _midiManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        [CanBeNull]
        private IMidiRenderer _renderer;
        private int _instrumentProgram = 1;

        [ViewVariables]
        private readonly Queue<ValueTuple<MidiEvent, double>> _midiQueue = new Queue<(MidiEvent, double)>();

        [ViewVariables]
        private float _timer = 0f;

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
            get => _renderer?.LoopMidi ?? false;
            set
            {
                if (_renderer != null)
                {
                    _renderer.LoopMidi = value;
                }
            }
        }

        /// <summary>
        ///     Changes the instrument the midi renderer will play.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int InstrumentProgram
        {
            get => _instrumentProgram;
            set
            {
                _instrumentProgram = value;
                if (_renderer != null)
                {
                    _renderer.MidiProgram = _instrumentProgram;
                }
            }
        }

        /// <summary>
        ///     Whether there's a midi song being played or not.
        /// </summary>
        [ViewVariables]
        public bool IsMidiOpen => _renderer?.Status == MidiRendererStatus.File;

        /// <summary>
        ///     Whether the midi renderer is listening for midi input or not.
        /// </summary>
        [ViewVariables]
        public bool IsInputOpen => _renderer?.Status == MidiRendererStatus.Input;

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _renderer = _midiManager.GetNewRenderer();

            if (_renderer != null)
            {
                _renderer.MidiProgram = _instrumentProgram;
                _renderer.TrackingEntity = Owner;
                _renderer.OnMidiPlayerFinished += () => { OnMidiPlaybackEnded?.Invoke(); };
            }
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

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (_renderer == null)
            {
                return;
            }

            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMessage:
                    // If we're the ones sending the MidiEvents, we ignore this message.
                    if (IsInputOpen || IsMidiOpen) break;
                    for (var i = 0; i < midiEventMessage.MidiEvent.Length; i++)
                    {
                        //_midiQueue.Enqueue((midiEventMessage.MidiEvent[i], (i == 0 ? 0 : 0) + _gameTiming.CurTime.TotalSeconds - midiEventMessage.Timestamp[i]));

                        var j = i;
                        Timer.Spawn((int) ((TimeBetweenNetMessages)*1.5f + _gameTiming.CurTime.TotalSeconds - midiEventMessage.Timestamp[i])*1000,
                            () => _renderer.SendMidiEvent(midiEventMessage.MidiEvent[j]));
                    }
                    break;

                case InstrumentStopMidiMessage _:
                    _renderer.StopAllNotes();
                    if (IsInputOpen) CloseInput();
                    if (IsMidiOpen) CloseMidi();
                    break;
            }
        }

        /// <inheritdoc cref="MidiRenderer.OpenInput"/>
        public bool OpenInput()
        {
            if (_renderer != null && _renderer.OpenInput())
            {
                _renderer.OnMidiEvent += RendererOnMidiEvent;
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="MidiRenderer.CloseInput"/>
        public bool CloseInput()
        {
            if (_renderer == null || !_renderer.CloseInput())
            {
                return false;
            }

            _renderer.OnMidiEvent -= RendererOnMidiEvent;
            return true;
        }

        /// <inheritdoc cref="MidiRenderer.OpenMidi(string)"/>
        public bool OpenMidi(string filename)
        {
            if (_renderer == null || !_renderer.OpenMidi(filename))
            {
                return false;
            }

            _renderer.OnMidiEvent += RendererOnMidiEvent;
            return true;
        }

        /// <inheritdoc cref="MidiRenderer.CloseMidi"/>
        public bool CloseMidi()
        {
            if (_renderer == null || !_renderer.CloseMidi())
            {
                return false;
            }

            _renderer.OnMidiEvent -= RendererOnMidiEvent;
            return true;
        }

        /// <summary>
        ///     Called whenever the renderer receives a midi event.
        /// </summary>
        /// <param name="midiEvent">The received midi event</param>
        private void RendererOnMidiEvent(MidiEvent midiEvent)
        {
            _midiQueue.Enqueue((midiEvent, _gameTiming.CurTime.TotalSeconds));
        }

        public void Update(float delta)
        {
            _timer -= delta;

            if (_timer > 0f) return;

            if (!IsMidiOpen && !IsInputOpen)
            {
                //UpdatePlaying(delta);
                return;
            }

            SendAllMidiMessages();
            _timer = TimeBetweenNetMessages;
        }

        private void UpdatePlaying(float delta)
        {
            if(_renderer == null || _midiQueue.Count == 0) return;
            var (midiEvent, timestamp) = _midiQueue.Dequeue();
            _renderer.SendMidiEvent(midiEvent);
            _timer = _midiQueue.Count != 0 ? (float) (_midiQueue.Peek().Item2) : 0;
        }

        private void SendAllMidiMessages()
        {
            var count = _midiQueue.Count;
            var events = new MidiEvent[count];
            var timestamps = new double[count];

            for (var i = 0; i < count; i++)
            {
                var (midiEvent, timestamp) = _midiQueue.Dequeue();
                events[i] = midiEvent;
                timestamps[i] = timestamp;
            }

            SendNetworkMessage(new InstrumentMidiEventMessage(events, timestamps));
        }
    }
}
