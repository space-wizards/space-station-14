using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Instruments;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Instruments;

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

        Subs.CVar(_cfg, CCVars.MaxMidiEventsPerBatch, OnMaxMidiEventsPerBatchChanged, true);
        Subs.CVar(_cfg, CCVars.MaxMidiEventsPerSecond, OnMaxMidiEventsPerSecondChanged, true);

        SubscribeNetworkEvent<InstrumentMidiEventEvent>(OnMidiEventRx);
        SubscribeNetworkEvent<InstrumentStartMidiEvent>(OnMidiStart);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(OnMidiStop);

        SubscribeLocalEvent<InstrumentComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InstrumentComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, SharedInstrumentComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not InstrumentComponentState state)
            return;

        component.Playing = state.Playing;
        component.InstrumentProgram = state.InstrumentProgram;
        component.InstrumentBank = state.InstrumentBank;
        component.AllowPercussion = state.AllowPercussion;
        component.AllowProgramChange = state.AllowProgramChange;
        component.RespectMidiLimits = state.RespectMidiLimits;
        component.Master = EnsureEntity<InstrumentComponent>(state.Master, uid);
        component.FilteredChannels = state.FilteredChannels;

        if (component.Playing)
            SetupRenderer(uid, true, component);
        else
            EndRenderer(uid, true, component);
    }

    private void OnShutdown(EntityUid uid, InstrumentComponent component, ComponentShutdown args)
    {
        EndRenderer(uid, false, component);
    }

    public void SetMaster(EntityUid uid, EntityUid? masterUid)
    {
        if (!HasComp<InstrumentComponent>(uid))
            return;

        RaiseNetworkEvent(new InstrumentSetMasterEvent(GetNetEntity(uid), GetNetEntity(masterUid)));
    }

    public void SetFilteredChannel(EntityUid uid, int channel, bool value)
    {
        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        if(value)
            instrument.Renderer?.SendMidiEvent(RobustMidiEvent.AllNotesOff((byte)channel, 0), false);

        RaiseNetworkEvent(new InstrumentSetFilteredChannelEvent(GetNetEntity(uid), channel, value));
    }

    public override bool ResolveInstrument(EntityUid uid, ref SharedInstrumentComponent? component)
    {
        if (component is not null)
            return true;

        TryComp<InstrumentComponent>(uid, out var localComp);
        component = localComp;
        return component != null;
    }

    public override void SetupRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? component = null)
    {
        if (!ResolveInstrument(uid, ref component))
            return;

        if (component is not InstrumentComponent instrument)
        {
            return;
        }

        if (instrument.IsRendererAlive)
        {
            if (fromStateChange)
            {
                UpdateRenderer(uid, instrument);
            }

            return;
        }

        instrument.SequenceDelay = 0;
        instrument.SequenceStartTick = 0;
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
            RaiseNetworkEvent(new InstrumentStartMidiEvent(GetNetEntity(uid)));
        }
    }

    public void UpdateRenderer(EntityUid uid, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument) || instrument.Renderer == null)
            return;

        instrument.Renderer.TrackingEntity = uid;

        instrument.Renderer.FilteredChannels.SetAll(false);
        instrument.Renderer.FilteredChannels.Or(instrument.FilteredChannels);

        instrument.Renderer.DisablePercussionChannel = !instrument.AllowPercussion;
        instrument.Renderer.DisableProgramChangeEvent = !instrument.AllowProgramChange;

        for (int i = 0; i < RobustMidiEvent.MaxChannels; i++)
        {
            if(instrument.FilteredChannels[i])
                instrument.Renderer.SendMidiEvent(RobustMidiEvent.AllNotesOff((byte)i, 0));
        }

        if (!instrument.AllowProgramChange)
        {
            instrument.Renderer.MidiBank = instrument.InstrumentBank;
            instrument.Renderer.MidiProgram = instrument.InstrumentProgram;
        }

        UpdateRendererMaster(instrument);

        instrument.Renderer.LoopMidi = instrument.LoopMidi;
    }

    private void UpdateRendererMaster(InstrumentComponent instrument)
    {
        if (instrument.Renderer == null || instrument.Master == null)
            return;

        if (!TryComp(instrument.Master, out InstrumentComponent? masterInstrument) || masterInstrument.Renderer == null)
            return;

        instrument.Renderer.Master = masterInstrument.Renderer;
    }

    public override void EndRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? component = null)
    {
        if (!ResolveInstrument(uid, ref component))
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

        instrument.Renderer?.SystemReset();
        instrument.Renderer?.ClearAllEvents();

        var renderer = instrument.Renderer;

        // We dispose of the synth two seconds from now to allow the last notes to stop from playing.
        // Don't use timers bound to the entity in case it is getting deleted.
        if (renderer != null)
            Timer.Spawn(2000, () => { renderer.Dispose(); });

        instrument.Renderer = null;
        instrument.MidiEventBuffer.Clear();

        if (!fromStateChange && _netManager.IsConnected)
        {
            RaiseNetworkEvent(new InstrumentStopMidiEvent(GetNetEntity(uid)));
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

        if (instrument.Renderer == null || !instrument.Renderer.OpenInput())
            return false;

        SetMaster(uid, null);
        instrument.MidiEventBuffer.Clear();
        instrument.Renderer.OnMidiEvent += instrument.MidiEventBuffer.Add;
        return true;

    }

    public bool OpenMidi(EntityUid uid, ReadOnlySpan<byte> data, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return false;

        SetupRenderer(uid, false, instrument);

        if (instrument.Renderer == null || !instrument.Renderer.OpenMidi(data))
            return false;

        SetMaster(uid, null);
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
        var uid = GetEntity(midiEv.Uid);

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

        SendMidiEvents(midiEv.MidiEvent, instrument);
    }

    private void SendMidiEvents(IReadOnlyList<RobustMidiEvent> midiEvents, InstrumentComponent instrument)
    {
        if (instrument.Renderer == null)
        {
            Log.Warning($"Tried to send Midi events to an instrument without a renderer.");
            return;
        }

        var currentTick = instrument.Renderer.SequencerTick;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (uint i = 0; i < midiEvents.Count; i++)
        {
            // I am surprised this doesn't take uint...
            var ev = midiEvents[(int)i];

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
        SetupRenderer(GetEntity(ev.Uid), true);
    }

    private void OnMidiStop(InstrumentStopMidiEvent ev)
    {
        EndRenderer(GetEntity(ev.Uid), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
        {
            return;
        }

        var query = EntityQueryEnumerator<InstrumentComponent>();
        while (query.MoveNext(out var uid, out var instrument))
        {
            // For cases where the master renderer was not created yet.
            if (instrument is { Renderer.Master: null, Master: not null })
                UpdateRendererMaster(instrument);

            if (instrument is { IsMidiOpen: false, IsInputOpen: false })
                continue;

            var now = _gameTiming.RealTime;
            var oneSecAGo = now.Add(OneSecAgo);

            if (instrument.LastMeasured <= oneSecAGo)
            {
                instrument.LastMeasured = now;
                instrument.SentWithinASec = 0;
            }

            if (instrument.MidiEventBuffer.Count == 0)
                continue;

            var max = instrument.RespectMidiLimits
                ? Math.Min(MaxMidiEventsPerBatch, MaxMidiEventsPerSecond - instrument.SentWithinASec)
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

            RaiseNetworkEvent(new InstrumentMidiEventEvent(GetNetEntity(uid), events));

            instrument.SentWithinASec += eventCount;

            instrument.MidiEventBuffer.RemoveRange(0, eventCount);
        }
    }
}
