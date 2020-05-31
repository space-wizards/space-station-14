using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Instruments;
using Content.Shared.Physics;
using JetBrains.Annotations;
using NFluidsynth;
using Robust.Shared.GameObjects;
using Robust.Client.Audio.Midi;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;
using MidiEvent = Robust.Shared.Audio.Midi.MidiEvent;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components.Instruments
{

    [RegisterComponent]
    public class InstrumentComponent : SharedInstrumentComponent
    {

        private const float SequenceDelaySeconds = .4f;

        /// <summary>
        ///     Called when a midi song stops playing.
        /// </summary>
        public event Action OnMidiPlaybackEnded;

#pragma warning disable 649
        [Dependency] private IMidiManager _midiManager;

        [Dependency] private IGameTiming _gameTiming;

        [Dependency] private ILogManager _logger;
#pragma warning restore 649

        private ISawmill _midiSawmill;

        [CanBeNull]
        private IMidiRenderer _renderer;

        private byte _instrumentProgram = 1;
        private byte _instrumentBank = 0;
        private uint _syncSequencerTick;

        /// <summary>
        ///     A queue of MidiEvents to be sent to the server.
        /// </summary>
        [ViewVariables]
        private List<MidiEvent> _midiQueue = new List<MidiEvent>();

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
        ///     Changes the instrument bank the midi renderer will use.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public byte InstrumentBank
        {
            get => _instrumentBank;
            set
            {
                _instrumentBank = value;
                if (_renderer != null)
                {
                    _renderer.MidiBank = _instrumentBank;
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
            _midiSawmill = _logger.GetSawmill("midi");
        }

        protected void SetupRenderer()
        {
            if (IsRendererAlive)
                return;

            _midiManager.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
            _renderer = _midiManager.GetNewRenderer();

            if (_renderer != null)
            {
                _renderer.MidiBank = _instrumentBank;
                _renderer.MidiProgram = _instrumentProgram;
                _renderer.TrackingEntity = Owner;
                _renderer.OnMidiPlayerFinished += () =>
                {
                    OnMidiPlaybackEnded?.Invoke();
                    EndRenderer();
                    SendNetworkMessage(new InstrumentStopMidiMessage());
                };
            }
        }

        protected void EndRenderer()
        {
            if (IsInputOpen)
                CloseInput();

            if (IsMidiOpen)
                CloseMidi();

            _renderer?.StopAllNotes();

            var renderer = _renderer;

            // We dispose of the synth two seconds from now to allow the last notes to stop from playing.
            Timer.Spawn(2000, () => { renderer?.Dispose(); });
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
            serializer.DataField(ref _instrumentProgram, "program", (byte) 1);
            serializer.DataField(ref _instrumentBank, "bank", (byte)0);
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMessage:
                    // If we're the ones sending the MidiEvents, we ignore this message.
                    if (!IsRendererAlive || IsInputOpen || IsMidiOpen) break;

                    foreach (var ev in midiEventMessage.MidiEvent)
                    {
                        var delta = (uint)(_renderer!.SequencerTimeScale * SequenceDelaySeconds)
                            + (ev.Tick - _syncSequencerTick);
                        _renderer?.ScheduleMidiEvent(ev, delta, true);
                    }

                    break;
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is InstrumentState state)) return;

            if (state.Playing)
            {
                SetupRenderer();
                _syncSequencerTick = state.SequencerTick;
            }
            else
                EndRenderer();
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
            _midiQueue.Add(midiEvent);
        }


        private TimeSpan _lastMeasuredInterval = TimeSpan.MinValue;

        private int _sentWithinASec = 0;

        private static readonly TimeSpan OneSecAgo = TimeSpan.FromSeconds(-1);

        public override void Update(float delta)
        {
            if (!IsMidiOpen && !IsInputOpen) return;

            var now = _gameTiming.RealTime;
            var oneSecAGo = now.Add(OneSecAgo);

            if (_lastMeasuredInterval <= oneSecAGo)
            {
                _lastMeasuredInterval = now;
                _sentWithinASec = 0;
            }

            if (_midiQueue.Count == 0) return;

            var max = Math.Min(MaxMidiEventsPerBatch, MaxMidiEventsPerSecond - _sentWithinASec);

            if (max <= 0)
            {
                // hit event/sec limit
                return;
            }

            var events = _midiQueue.Distinct().Take(max).ToArray();

            SendNetworkMessage(new InstrumentMidiEventMessage(events));

            var eventCount = events.Length;

            _sentWithinASec += eventCount;

            _midiQueue.RemoveRange(0, eventCount);
        }

    }

}
