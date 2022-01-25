using System;
using System.Linq;
using Content.Server.Stunnable;
using Content.Server.UserInterface;
using Content.Shared.Instruments;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Instruments;

[UsedImplicitly]
public sealed partial class InstrumentSystem : SharedInstrumentSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeCVars();

        SubscribeNetworkEvent<InstrumentMidiEventEvent>(OnMidiEventRx);
        SubscribeNetworkEvent<InstrumentStartMidiEvent>(OnMidiStart);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(OnMidiStop);

        SubscribeLocalEvent<InstrumentComponent, ActivatableUIPlayerChangedEvent>(InstrumentNeedsClean);
    }

    private void OnMidiStart(InstrumentStartMidiEvent msg, EntitySessionEventArgs args)
    {
        var uid = msg.Uid;

        if (!EntityManager.TryGetComponent(uid, out InstrumentComponent? instrument))
            return;

        if (args.SenderSession != instrument.InstrumentPlayer)
            return;

        instrument.Playing = true;
        instrument.Dirty();
    }

    private void OnMidiStop(InstrumentStopMidiEvent msg, EntitySessionEventArgs args)
    {
        var uid = msg.Uid;

        if (!EntityManager.TryGetComponent(uid, out InstrumentComponent? instrument))
            return;

        if (args.SenderSession != instrument.InstrumentPlayer)
            return;

        Clean(uid, instrument);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ShutdownCVars();
    }

    public void Clean(EntityUid uid, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return;

        if (instrument.Playing)
        {
            RaiseNetworkEvent(new InstrumentStopMidiEvent(uid));
        }

        instrument.Playing = false;
        instrument.LastSequencerTick = 0;
        instrument.BatchesDropped = 0;
        instrument.LaggedBatches = 0;
        instrument.Dirty();
    }

    private void InstrumentNeedsClean(EntityUid uid, InstrumentComponent component, ActivatableUIPlayerChangedEvent ev)
    {
        Clean(uid, component);
    }

    private void OnMidiEventRx(InstrumentMidiEventEvent msg, EntitySessionEventArgs args)
    {
        var uid = msg.Uid;

        if (!EntityManager.TryGetComponent(uid, out InstrumentComponent? instrument))
            return;

        if (!instrument.Playing
            || args.SenderSession != instrument.InstrumentPlayer
            || instrument.InstrumentPlayer == null
            || args.SenderSession.AttachedEntity is not {} attached)
            return;

        var send = true;

        var minTick = msg.MidiEvent.Min(x => x.Tick);
        if (instrument.LastSequencerTick > minTick)
        {
            instrument.LaggedBatches++;

            if (instrument.RespectMidiLimits)
            {
                if (instrument.LaggedBatches == (int) (MaxMidiLaggedBatches * (1 / 3d) + 1))
                {
                    attached.PopupMessage(
                        Loc.GetString("instrument-component-finger-cramps-light-message"));
                } else if (instrument.LaggedBatches == (int) (MaxMidiLaggedBatches * (2 / 3d) + 1))
                {
                    attached.PopupMessage(
                        Loc.GetString("instrument-component-finger-cramps-serious-message"));
                }
            }

            if (instrument.LaggedBatches > MaxMidiLaggedBatches)
            {
                send = false;
            }
        }

        if (++instrument.MidiEventCount > MaxMidiEventsPerSecond
            || msg.MidiEvent.Length > MaxMidiEventsPerBatch)
        {
            instrument.BatchesDropped++;

            send = false;
        }

        if (send || !instrument.RespectMidiLimits)
        {
            RaiseNetworkEvent(msg);
        }

        var maxTick = msg.MidiEvent.Max(x => x.Tick);
        instrument.LastSequencerTick = Math.Max(maxTick, minTick);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var instrument in EntityManager.EntityQuery<InstrumentComponent>(true))
        {
            if (instrument.DirtyRenderer)
            {
                instrument.Dirty();
                instrument.DirtyRenderer = false;
            }

            if ((instrument.BatchesDropped >= MaxMidiBatchesDropped
                 || instrument.LaggedBatches >= MaxMidiLaggedBatches)
                && instrument.InstrumentPlayer != null && instrument.RespectMidiLimits)
            {
                // Just in case
                Clean((instrument).Owner);
                instrument.UserInterface?.CloseAll();

                if (instrument.InstrumentPlayer.AttachedEntity is {Valid: true} mob)
                {
                    _stunSystem.TryParalyze(mob, TimeSpan.FromSeconds(1), true);

                    instrument.Owner.PopupMessage(mob, "instrument-component-finger-cramps-max-message");
                }
            }

            instrument.Timer += frameTime;
            if (instrument.Timer < 1)
                continue;

            instrument.Timer = 0f;
            instrument.MidiEventCount = 0;
            instrument.LaggedBatches = 0;
            instrument.BatchesDropped = 0;
        }
    }
}
