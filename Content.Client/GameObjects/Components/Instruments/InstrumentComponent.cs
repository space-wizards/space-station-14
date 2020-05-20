using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Instruments;
using JetBrains.Annotations;
using NFluidsynth;
using Robust.Shared.GameObjects;
using Robust.Client.Audio.Midi;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;
using MidiEvent = Robust.Shared.Audio.Midi.MidiEvent;
using Timer = Robust.Shared.Timers.Timer;


namespace Content.Client.GameObjects.Components.Instruments
{
    [RegisterComponent]
    public class InstrumentComponent : SharedInstrumentComponent
    {
        public const float TimeBetweenNetMessages = 1.0f;

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
        private byte _instrumentProgram = 1;

        /// <summary>
        ///     A queue of MidiEvents to be sent to the server.
        /// </summary>
        [ViewVariables]
        private readonly Queue<MidiEvent> _midiQueue = new Queue<MidiEvent>();

        [ViewVariables]
        private float _timer = 0f;

        private TimeSpan? _lastEvent = null;

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
        public byte InstrumentProgram
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

        /// <summary>
        ///     Whether the midi renderer is alive or not.
        /// </summary>
        [ViewVariables]
        public bool IsRendererAlive => _renderer != null;

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
        }

        protected void SetupRenderer()
        {
            if (IsRendererAlive)
                return;

            _renderer = _midiManager.GetNewRenderer();

            if (_renderer != null)
            {
                _renderer.MidiProgram = _instrumentProgram;
                _renderer.TrackingEntity = Owner;
                _renderer.OnMidiPlayerFinished += () => { OnMidiPlaybackEnded?.Invoke(); EndRenderer(); SendNetworkMessage(new InstrumentStopMidiMessage()); };
            }
        }

        protected void EndRenderer()
        {
            if (IsInputOpen)
                CloseInput();

            if (IsMidiOpen)
                CloseMidi();

            var renderer = _renderer;
            Timer.Spawn(1000, () => { renderer?.Dispose(); });
            _renderer = null;
            _midiQueue.Clear();
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            EndRenderer();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _instrumentProgram, "program", (byte)1);
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMessage:
                    // If we're the ones sending the MidiEvents, we ignore this message.
                    if (!IsRendererAlive || IsInputOpen || IsMidiOpen) break;
                    var curTime = _gameTiming.CurTime;
                    Logger.Info($"NEW BATCH!!! LENGTH:{midiEventMessage.MidiEvent.Length} QUEUED:{_midiQueue.Count} LAST:{_lastEvent}");
                    for (var i = 0; i < midiEventMessage.MidiEvent.Length; i++)
                    {
                        var ev = midiEventMessage.MidiEvent[i];
                        var delta = i != 0 ?
                            ev.Timestamp.Subtract(midiEventMessage.MidiEvent[i-1].Timestamp) : _lastEvent.HasValue ? ev.Timestamp.Subtract(_lastEvent.Value) :  TimeSpan.Zero;
                        ev.Timestamp = curTime + TimeSpan.FromSeconds(TimeBetweenNetMessages*1.25);
                        Logger.Info($"DT:{delta} TIM:{ev.Timestamp} TIMR:{midiEventMessage.MidiEvent[i].Timestamp} LST:{midiEventMessage.MidiEvent[Math.Max(0, i-1)].Timestamp}");
                        _midiQueue.Enqueue(ev);
                        _lastEvent = ev.Timestamp;

                        //var j = i;
                        //Timer.Spawn((int)ev.Timestamp.Subtract(_gameTiming.CurTime).TotalMilliseconds,
                        //    () => _renderer?.SendMidiEvent(midiEventMessage.MidiEvent[j]));
                    }



                    break;

                case InstrumentStopMidiMessage _:
                    EndRenderer();
                    break;

                case InstrumentStartMidiMessage _:
                    SetupRenderer();
                    Logger.Info("INITIALIZED MIDI RENDERER. I HOPE.");
                    break;
            }
        }

        /// <inheritdoc cref="MidiRenderer.OpenInput"/>
        public bool OpenInput()
        {
            SetupRenderer();
            SendNetworkMessage(new InstrumentStartMidiMessage());

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

            EndRenderer();
            SendNetworkMessage(new InstrumentStopMidiMessage());
            return true;
        }

        /// <inheritdoc cref="MidiRenderer.OpenMidi(string)"/>
        public bool OpenMidi(string filename)
        {
            SetupRenderer();
            SendNetworkMessage(new InstrumentStartMidiMessage());

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

            EndRenderer();
            SendNetworkMessage(new InstrumentStopMidiMessage());
            return true;
        }

        /// <summary>
        ///     Called whenever the renderer receives a midi event.
        /// </summary>
        /// <param name="midiEvent">The received midi event</param>
        private void RendererOnMidiEvent(MidiEvent midiEvent)
        {
            midiEvent.Timestamp = _gameTiming.CurTime;
            _midiQueue.Enqueue(midiEvent);
        }

        public override void Update(float delta)
        {
            _timer -= delta;

            if (_timer > 0f) return;

            if (!IsMidiOpen && !IsInputOpen)
            {
                UpdatePlaying(delta);
                return;
            }

            SendAllMidiMessages();
            _timer = TimeBetweenNetMessages;
        }

        private void UpdatePlaying(float delta)
        {
            while (true)
            {
                if (_renderer == null || _midiQueue.Count == 0) return;
                var midiEvent = _midiQueue.Dequeue();
                _renderer.SendMidiEvent(midiEvent);
                _timer = _midiQueue.Count != 0 ? (MathF.Max((float) _midiQueue.Peek().Timestamp.Subtract(_gameTiming.CurTime).TotalSeconds, 0f)) : 0;
                if (_timer <= 0f) continue;
                break;
            }
        }

        private void SendAllMidiMessages()
        {
            var count = _midiQueue.Count;
            if (count == 0) return;
            var events = _midiQueue.ToArray();
            _midiQueue.Clear();

            SendNetworkMessage(new InstrumentMidiEventMessage(events));
        }
    }
}
