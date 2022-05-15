using Content.Server.Stunnable;
using Content.Server.UserInterface;
using Content.Shared.Instruments;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.Instruments;

[UsedImplicitly]
public sealed partial class InstrumentSystem : SharedInstrumentSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeCVars();

        SubscribeNetworkEvent<InstrumentMidiEventEvent>(OnMidiEventRx);
        SubscribeNetworkEvent<InstrumentStartMidiEvent>(OnMidiStart);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(OnMidiStop);

        SubscribeLocalEvent<InstrumentComponent, BoundUIClosedEvent>(OnBoundUIClosed);
        SubscribeLocalEvent<InstrumentComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
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

    private void OnBoundUIClosed(EntityUid uid, InstrumentComponent component, BoundUIClosedEvent args)
    {
        if (args.UiKey is not InstrumentUiKey)
            return;

        if (HasComp<ActiveInstrumentComponent>(uid)
            && _userInterfaceSystem.TryGetUi(uid, args.UiKey, out var bui)
            && bui.SubscribedSessions.Count == 0)
        {
            RemComp<ActiveInstrumentComponent>(uid);
        }

        Clean(uid, component);
    }

    private void OnBoundUIOpened(EntityUid uid, InstrumentComponent component, BoundUIOpenedEvent args)
    {
        if (args.UiKey is not InstrumentUiKey)
            return;

        EnsureComp<ActiveInstrumentComponent>(uid);
        Clean(uid, component);
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

    private void OnMidiEventRx(InstrumentMidiEventEvent msg, EntitySessionEventArgs args)
    {
        var uid = msg.Uid;

        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        if (!instrument.Playing
            || args.SenderSession != instrument.InstrumentPlayer
            || instrument.InstrumentPlayer == null
            || args.SenderSession.AttachedEntity is not {} attached)
            return;

        var send = true;

        var minTick = uint.MaxValue;
        var maxTick = uint.MinValue;

        for (var i = 0; i < msg.MidiEvent.Length; i++)
        {
            var tick = msg.MidiEvent[i].Tick;

            if (tick < minTick)
                minTick = tick;

            if (tick > maxTick)
                maxTick = tick;
        }

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

        instrument.LastSequencerTick = Math.Max(maxTick, minTick);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (_, instrument) in EntityManager.EntityQuery<ActiveInstrumentComponent, InstrumentComponent>(true))
        {
            if (instrument.DirtyRenderer)
            {
                Dirty(instrument);
                instrument.DirtyRenderer = false;
            }

            if (instrument.RespectMidiLimits &&
                (instrument.BatchesDropped >= MaxMidiBatchesDropped
                 || instrument.LaggedBatches >= MaxMidiLaggedBatches))
            {
                if (instrument.InstrumentPlayer?.AttachedEntity is {Valid: true} mob)
                {
                    _stunSystem.TryParalyze(mob, TimeSpan.FromSeconds(1), true);

                    instrument.Owner.PopupMessage(mob, Loc.GetString("instrument-component-finger-cramps-max-message"));
                }

                // Just in case
                Clean((instrument).Owner);
                instrument.UserInterface?.CloseAll();
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

    public void ToggleInstrumentUi(EntityUid uid, IPlayerSession session, InstrumentComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var ui = uid.GetUIOrNull(InstrumentUiKey.Key);
        ui?.Toggle(session);
    }
}
