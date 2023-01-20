using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Instruments;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Instruments;

[UsedImplicitly]
public sealed class InstrumentSystem : SharedInstrumentSystem
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IMidiManager _midiManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public readonly TimeSpan OneSecAgo = TimeSpan.FromSeconds(-1);
    public int MaxMidiEventsPerBatch { get; private set; }
    public int MaxMidiEventsPerSecond { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        _cfg.OnValueChanged(CCVars.MaxMidiEventsPerBatch, OnMaxMidiEventsPerBatchChanged, true);
        _cfg.OnValueChanged(CCVars.MaxMidiEventsPerSecond, OnMaxMidiEventsPerSecondChanged, true);

        SubscribeNetworkEvent<InstrumentMidiEventEvent>(OnMidiEventRx);
        SubscribeNetworkEvent<InstrumentStartMidiEvent>(OnMidiStart);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(OnMidiStop);

        SubscribeLocalEvent<InstrumentComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.MaxMidiEventsPerBatch, OnMaxMidiEventsPerBatchChanged);
        _cfg.UnsubValueChanged(CCVars.MaxMidiEventsPerSecond, OnMaxMidiEventsPerSecondChanged);
    }

    private void OnShutdown(EntityUid uid, InstrumentComponent component, ComponentShutdown args)
    {
        EndRenderer(uid, false, component);
    }

    public override void SetupRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component is not InstrumentComponent instrument || instrument.IsRendererAlive)
            return;

        instrument.SequenceDelay = 0;
        instrument.SequenceStartTick = 0;
        _midiManager.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
        instrument.Renderer = _midiManager.GetNewRenderer();

        if (instrument.Renderer != null)
        {
            instrument.Renderer.SendMidiEvent(RobustMidiEvent.SystemReset(instrument.Renderer.SequencerTick));
            UpdateRenderer(uid, instrument);
            instrument.Renderer.OnMidiPlayerFinished += () =>
            {
                instrument.PlaybackEndedInvoke();
                EndRenderer(uid, fromStateChange, instrument);
            };
        }

        if (!fromStateChange)
        {
            RaiseNetworkEvent(new InstrumentStartMidiEvent(uid));
        }
    }

    public void UpdateRenderer(EntityUid uid, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument) || instrument.Renderer == null)
            return;

        instrument.Renderer.TrackingEntity = instrument.Owner;
        instrument.Renderer.DisablePercussionChannel = !instrument.AllowPercussion;
        instrument.Renderer.DisableProgramChangeEvent = !instrument.AllowProgramChange;

        if (!instrument.AllowProgramChange)
        {
            instrument.Renderer.MidiBank = instrument.InstrumentBank;
            instrument.Renderer.MidiProgram = instrument.InstrumentProgram;
        }

        instrument.Renderer.LoopMidi = instrument.LoopMidi;
        instrument.DirtyRenderer = false;
    }

    public override void EndRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component is not InstrumentComponent instrument)
            return;

        if (instrument.IsInputOpen)
        {
            CloseInput(uid, fromStateChange, instrument);
            return;
        }

        if (instrument.IsMidiOpen)
        {
            CloseMidi(uid, fromStateChange, instrument);
            return;
        }

        instrument.Renderer?.StopAllNotes();

        var renderer = instrument.Renderer;

        // We dispose of the synth two seconds from now to allow the last notes to stop from playing.
        // Don't use timers bound to the entity in case it is getting deleted.
        Timer.Spawn(2000, () => { renderer?.Dispose(); });
        instrument.Renderer = null;
        instrument.MidiEventBuffer.Clear();

        if (!fromStateChange && _netManager.IsConnected)
        {
            RaiseNetworkEvent(new InstrumentStopMidiEvent(uid));
        }
    }

    public void SetPlayerTick(EntityUid uid, int playerTick, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return;

        if (instrument.Renderer is not { Status: MidiRendererStatus.File })
            return;

        instrument.MidiEventBuffer.Clear();

        var tick = instrument.Renderer.SequencerTick-1;

        instrument.MidiEventBuffer.Add(RobustMidiEvent.SystemReset(tick));
        instrument.Renderer.PlayerTick = playerTick;
    }

    public bool OpenInput(EntityUid uid, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument, false))
            return false;

        SetupRenderer(uid, false, instrument);

        if (instrument.Renderer != null && instrument.Renderer.OpenInput())
        {
            instrument.Renderer.OnMidiEvent += instrument.MidiEventBuffer.Add;
            return true;
        }

        return false;
    }

    public bool OpenMidi(EntityUid uid, ReadOnlySpan<byte> data, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return false;

        SetupRenderer(uid, false, instrument);

        if (instrument.Renderer == null || !instrument.Renderer.OpenMidi(data))
        {
            return false;
        }

        instrument.MidiEventBuffer.Clear();

        instrument.Renderer.OnMidiEvent += instrument.MidiEventBuffer.Add;
        return true;
    }

    public bool CloseInput(EntityUid uid, bool fromStateChange, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return false;

        if (instrument.Renderer == null || !instrument.Renderer.CloseInput())
        {
            return false;
        }

        EndRenderer(uid, fromStateChange, instrument);
        return true;
    }

    public bool CloseMidi(EntityUid uid, bool fromStateChange, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return false;

        if (instrument.Renderer == null || !instrument.Renderer.CloseMidi())
        {
            return false;
        }

        EndRenderer(uid, fromStateChange, instrument);
        return true;
    }

    private void OnMaxMidiEventsPerSecondChanged(int obj)
    {
        MaxMidiEventsPerSecond = obj;
    }

    private void OnMaxMidiEventsPerBatchChanged(int obj)
    {
        MaxMidiEventsPerBatch = obj;
    }

    private void OnMidiEventRx(InstrumentMidiEventEvent midiEv)
    {
        var uid = midiEv.Uid;

        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        var renderer = instrument.Renderer;

        if (renderer != null)
        {
            // If we're the ones sending the MidiEvents, we ignore this message.
            if (instrument.IsInputOpen || instrument.IsMidiOpen)
                return;
        }
        else
        {
            // if we haven't started or finished some sequence
            if (instrument.SequenceStartTick == 0)
            {
                // we may have arrived late
                SetupRenderer(uid, true, instrument);
            }

            // might be our own notes after we already finished playing
            return;
        }

        if (instrument.SequenceStartTick <= 0)
        {
            instrument.SequenceStartTick = midiEv.MidiEvent.Min(x => x.Tick) - 1;
        }

        var sqrtLag = MathF.Sqrt((_netManager.ServerChannel?.Ping ?? 0)/ 1000f);
        var delay = (uint) (renderer.SequencerTimeScale * (.2 + sqrtLag));
        var delta = delay - instrument.SequenceStartTick;

        instrument.SequenceDelay = Math.Max(instrument.SequenceDelay, delta);

        var currentTick = renderer.SequencerTick;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (uint i = 0; i < midiEv.MidiEvent.Length; i++)
        {
            var ev = midiEv.MidiEvent[i];

            var scheduled = ev.Tick + instrument.SequenceDelay;

            if (scheduled < currentTick)
            {
                instrument.SequenceDelay += currentTick - ev.Tick;
                scheduled = ev.Tick + instrument.SequenceDelay;
            }

            // The order of events with the same timestamp is undefined in Fluidsynth's sequencer...
            // Therefore we add the event index to the scheduled time to ensure every event has an unique timestamp.
            instrument.Renderer?.ScheduleMidiEvent(ev, scheduled+i, true);
        }
    }

    private void OnMidiStart(InstrumentStartMidiEvent ev)
    {
        SetupRenderer(ev.Uid, true);
    }

    private void OnMidiStop(InstrumentStopMidiEvent ev)
    {
        EndRenderer(ev.Uid, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
        {
            return;
        }

        foreach (var instrument in EntityManager.EntityQuery<InstrumentComponent>(true))
        {
            if (instrument.DirtyRenderer && instrument.Renderer != null)
                UpdateRenderer(instrument.Owner, instrument);

            if (!instrument.IsMidiOpen && !instrument.IsInputOpen)
                continue;

            var now = _gameTiming.RealTime;
            var oneSecAGo = now.Add(OneSecAgo);

            if (instrument.LastMeasured <= oneSecAGo)
            {
                instrument.LastMeasured = now;
                instrument.SentWithinASec = 0;
            }

            if (instrument.MidiEventBuffer.Count == 0) continue;

            var max = instrument.RespectMidiLimits ?
                Math.Min(MaxMidiEventsPerBatch, MaxMidiEventsPerSecond - instrument.SentWithinASec)
                : instrument.MidiEventBuffer.Count;

            if (max <= 0)
            {
                // hit event/sec limit, have to lag the batch or drop events
                continue;
            }

            // fix cross-fade events generating retroactive events
            // also handle any significant backlog of events after midi finished

            var bufferTicks = instrument.IsRendererAlive && instrument.Renderer!.Status != MidiRendererStatus.None
                ? instrument.Renderer.SequencerTimeScale * .2f
                : 0;

            var bufferedTick = instrument.IsRendererAlive
                ? instrument.Renderer!.SequencerTick - bufferTicks
                : int.MaxValue;

            // TODO: Remove LINQ brain-rot.
            var events = instrument.MidiEventBuffer
                .TakeWhile(x => x.Tick < bufferedTick)
                .Take(max)
                .ToArray();

            var eventCount = events.Length;

            if (eventCount == 0)
                continue;

            RaiseNetworkEvent(new InstrumentMidiEventEvent(instrument.Owner, events));

            instrument.SentWithinASec += eventCount;

            instrument.MidiEventBuffer.RemoveRange(0, eventCount);
        }
    }
}
