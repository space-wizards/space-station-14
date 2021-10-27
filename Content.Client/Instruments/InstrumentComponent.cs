using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Instruments;
using Content.Shared.Physics;
using Robust.Client;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Instruments
{

    [RegisterComponent]
    public class InstrumentComponent : SharedInstrumentComponent
    {

        /// <summary>
        ///     Called when a midi song stops playing.
        /// </summary>
        public event Action? OnMidiPlaybackEnded;

        [Dependency] private readonly IMidiManager _midiManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;

        private IMidiRenderer? _renderer;

        private InstrumentSystem _instrumentSystem = default!;

        [DataField("program")]
        private byte _instrumentProgram = 1;

        [DataField("bank")]
        private byte _instrumentBank;

        private uint _sequenceDelay;

        private uint _sequenceStartTick;

        [DataField("allowPercussion")]
        private bool _allowPercussion;

        [DataField("allowProgramChange")]
        private bool _allowProgramChange;

        [DataField("respectMidiLimits")]
        private bool _respectMidiLimits = true;

        /// <summary>
        ///     A queue of MidiEvents to be sent to the server.
        /// </summary>
        [ViewVariables]
        private readonly List<MidiEvent> _midiEventBuffer = new();

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
        public override byte InstrumentProgram
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
        public override byte InstrumentBank
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

        [ViewVariables(VVAccess.ReadWrite)]
        public override bool AllowPercussion
        {
            get => _allowPercussion;
            set
            {
                _allowPercussion = value;
                if (_renderer != null)
                {
                    _renderer.DisablePercussionChannel = !_allowPercussion;
                }
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public override bool AllowProgramChange
        {
            get => _allowProgramChange;
            set
            {
                _allowProgramChange = value;
                if (_renderer != null)
                {
                    _renderer.DisableProgramChangeEvent = !_allowProgramChange;
                }
            }
        }

        /// <summary>
        ///     Whether this instrument is handheld or not.
        /// </summary>
        [ViewVariables]
        [DataField("handheld")]
        public bool Handheld { get; set; } // TODO: Replace this by simply checking if the entity has an ItemComponent.

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

        [ViewVariables]
        public int PlayerTotalTick => _renderer?.PlayerTotalTick ?? 0;

        [ViewVariables]
        public int PlayerTick
        {
            get => _renderer?.PlayerTick ?? 0;
            set
            {
                if (!IsRendererAlive || _renderer!.Status != MidiRendererStatus.File) return;

                _midiEventBuffer.Clear();

                _renderer.PlayerTick = value;
                var tick = _renderer.SequencerTick;

                // We add a "all notes off" message.
                for (byte i = 0; i < 16; i++)
                {
                    _midiEventBuffer.Add(new MidiEvent()
                    {
                        Tick = tick, Type = 176,
                        Control = 123, Velocity = 0, Channel = i,
                    });
                }

                // Now we add a Reset All Controllers message.
                _midiEventBuffer.Add(new MidiEvent()
                {
                    Tick = tick, Type = 176,
                    Control = 121, Value = 0,
                });
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _instrumentSystem = EntitySystem.Get<InstrumentSystem>();
        }

        protected virtual void SetupRenderer(bool fromStateChange = false)
        {
            if (IsRendererAlive) return;

            _sequenceDelay = 0;
            _sequenceStartTick = 0;
            _midiManager.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
            _renderer = _midiManager.GetNewRenderer();

            if (_renderer != null)
            {
                _renderer.MidiBank = _instrumentBank;
                _renderer.MidiProgram = _instrumentProgram;
                _renderer.TrackingEntity = Owner;
                _renderer.DisablePercussionChannel = !_allowPercussion;
                _renderer.DisableProgramChangeEvent = !_allowProgramChange;
                _renderer.OnMidiPlayerFinished += () =>
                {
                    OnMidiPlaybackEnded?.Invoke();
                    EndRenderer(fromStateChange);
                };
            }

            if (!fromStateChange)
            {
#pragma warning disable 618
                SendNetworkMessage(new InstrumentStartMidiMessage());
#pragma warning restore 618
            }
        }

        protected void EndRenderer(bool fromStateChange = false)
        {
            if (IsInputOpen)
            {
                CloseInput(fromStateChange);
                return;
            }

            if (IsMidiOpen)
            {
                CloseMidi(fromStateChange);
                return;
            }

            _renderer?.StopAllNotes();

            var renderer = _renderer;

            // We dispose of the synth two seconds from now to allow the last notes to stop from playing.
            Owner.SpawnTimer(2000, () => { renderer?.Dispose(); });
            _renderer = null;
            _midiEventBuffer.Clear();

            if (!fromStateChange && IoCManager.Resolve<INetManager>().IsConnected)
            {
#pragma warning disable 618
                SendNetworkMessage(new InstrumentStopMidiMessage());
#pragma warning restore 618
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            EndRenderer();
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMessage:
                    if (IsRendererAlive)
                    {
                        // If we're the ones sending the MidiEvents, we ignore this message.
                        if (IsInputOpen || IsMidiOpen) break;
                    }
                    else
                    {
                        // if we haven't started or finished some sequence
                        if (_sequenceStartTick == 0)
                        {
                            // we may have arrived late
                            SetupRenderer(true);
                        }

                        // might be our own notes after we already finished playing
                        return;
                    }

                    if (_sequenceStartTick <= 0)
                    {
                        _sequenceStartTick = midiEventMessage.MidiEvent
                            .Min(x => x.Tick) - 1;
                    }

                    var sqrtLag = MathF.Sqrt(_netManager.ServerChannel!.Ping / 1000f);
                    var delay = (uint) (_renderer!.SequencerTimeScale * (.2 + sqrtLag));
                    var delta = delay - _sequenceStartTick;

                    _sequenceDelay = Math.Max(_sequenceDelay, delta);

                    var currentTick = _renderer.SequencerTick;

                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < midiEventMessage.MidiEvent.Length; i++)
                    {
                        var ev = midiEventMessage.MidiEvent[i];
                        var scheduled = ev.Tick + _sequenceDelay;

                        if (scheduled <= currentTick)
                        {
                            _sequenceDelay += currentTick - ev.Tick;
                            scheduled = ev.Tick + _sequenceDelay;
                        }


                        _renderer?.ScheduleMidiEvent(ev, scheduled, true);
                    }

                    break;
                case InstrumentStartMidiMessage _:
                {
                    SetupRenderer(true);
                    break;
                }
                case InstrumentStopMidiMessage _:
                {
                    EndRenderer(true);
                    break;
                }
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not InstrumentState state) return;

            if (state.Playing)
            {
                SetupRenderer(true);
            }
            else
            {
                EndRenderer(true);
            }

            AllowPercussion = state.AllowPercussion;
            AllowProgramChange = state.AllowProgramChange;
            InstrumentBank = state.InstrumentBank;
            InstrumentProgram = state.InstrumentProgram;
        }

        /// <inheritdoc cref="MidiRenderer.OpenInput"/>
        public bool OpenInput()
        {
            SetupRenderer();

            if (_renderer != null && _renderer.OpenInput())
            {
                _renderer.OnMidiEvent += RendererOnMidiEvent;
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="MidiRenderer.CloseInput"/>
        public bool CloseInput(bool fromStateChange = false)
        {
            if (_renderer == null || !_renderer.CloseInput())
            {
                return false;
            }

            EndRenderer(fromStateChange);
            return true;
        }

        public bool OpenMidi(ReadOnlySpan<byte> data)
        {
            SetupRenderer();

            if (_renderer == null || !_renderer.OpenMidi(data))
            {
                return false;
            }

            _renderer.OnMidiEvent += RendererOnMidiEvent;
            return true;
        }

        /// <inheritdoc cref="MidiRenderer.CloseMidi"/>
        public bool CloseMidi(bool fromStateChange = false)
        {
            if (_renderer == null || !_renderer.CloseMidi())
            {
                return false;
            }

            EndRenderer(fromStateChange);
            return true;
        }

        /// <summary>
        ///     Called whenever the renderer receives a midi event.
        /// </summary>
        /// <param name="midiEvent">The received midi event</param>
        private void RendererOnMidiEvent(MidiEvent midiEvent)
        {
            _midiEventBuffer.Add(midiEvent);
        }

        private TimeSpan _lastMeasured = TimeSpan.MinValue;

        private int _sentWithinASec;

        private static readonly TimeSpan OneSecAgo = TimeSpan.FromSeconds(-1);

        private static readonly Comparer<MidiEvent> SortMidiEventTick
            = Comparer<MidiEvent>.Create((x, y)
                => x.Tick.CompareTo(y.Tick));

        public override void Update(float delta)
        {
            if (!IsMidiOpen && !IsInputOpen) return;

            var now = _gameTiming.RealTime;
            var oneSecAGo = now.Add(OneSecAgo);

            if (_lastMeasured <= oneSecAGo)
            {
                _lastMeasured = now;
                _sentWithinASec = 0;
            }

            if (_midiEventBuffer.Count == 0) return;

            var max = _respectMidiLimits ?
                Math.Min(_instrumentSystem.MaxMidiEventsPerBatch, _instrumentSystem.MaxMidiEventsPerSecond - _sentWithinASec)
                : _midiEventBuffer.Count;

            if (max <= 0)
            {
                // hit event/sec limit, have to lag the batch or drop events
                return;
            }

            // fix cross-fade events generating retroactive events
            // also handle any significant backlog of events after midi finished

            _midiEventBuffer.Sort(SortMidiEventTick);
            var bufferTicks = IsRendererAlive && _renderer!.Status != MidiRendererStatus.None
                ? _renderer.SequencerTimeScale * .2f
                : 0;
            var bufferedTick = IsRendererAlive
                ? _renderer!.SequencerTick - bufferTicks
                : int.MaxValue;

            var events = _midiEventBuffer
                .TakeWhile(x => x.Tick < bufferedTick)
                .Take(max)
                .ToArray();

            var eventCount = events.Length;

            if (eventCount == 0) return;

#pragma warning disable 618
            SendNetworkMessage(new InstrumentMidiEventMessage(events));
#pragma warning restore 618

            _sentWithinASec += eventCount;

            _midiEventBuffer.RemoveRange(0, eventCount);
        }

    }

}
