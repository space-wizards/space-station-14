using Content.Server.Administration;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Administration;
using Content.Shared.Instruments;
using Content.Shared.Instruments.UI;
using Content.Shared.Physics;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Instruments;

[UsedImplicitly]
public sealed partial class InstrumentSystem : SharedInstrumentSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly StunSystem _stuns = default!;
    [Dependency] private readonly UserInterfaceSystem _bui = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly InteractionSystem _interactions = default!;

    private const float MaxInstrumentBandRange = 10f;

    // Band Requests are queued and delayed both to avoid metagaming and to prevent spamming it, since it's expensive.
    private const float BandRequestDelay = 1.0f;
    private TimeSpan _bandRequestTimer = TimeSpan.Zero;
    private readonly List<InstrumentBandRequestBuiMessage> _bandRequestQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeCVars();

        SubscribeNetworkEvent<InstrumentMidiEventEvent>(OnMidiEventRx);
        SubscribeNetworkEvent<InstrumentStartMidiEvent>(OnMidiStart);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(OnMidiStop);
        SubscribeNetworkEvent<InstrumentSetMasterEvent>(OnMidiSetMaster);
        SubscribeNetworkEvent<InstrumentSetFilteredChannelEvent>(OnMidiSetFilteredChannel);

        Subs.BuiEvents<InstrumentComponent>(InstrumentUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            subs.Event<InstrumentBandRequestBuiMessage>(OnBoundUIRequestBands);
        });

        SubscribeLocalEvent<InstrumentComponent, ComponentGetState>(OnStrumentGetState);

        _conHost.RegisterCommand("addtoband", AddToBandCommand);
    }

    private void OnStrumentGetState(EntityUid uid, InstrumentComponent component, ref ComponentGetState args)
    {
        args.State = new InstrumentComponentState()
        {
            Playing = component.Playing,
            InstrumentProgram = component.InstrumentProgram,
            InstrumentBank = component.InstrumentBank,
            AllowPercussion = component.AllowPercussion,
            AllowProgramChange = component.AllowProgramChange,
            RespectMidiLimits = component.RespectMidiLimits,
            Master = GetNetEntity(component.Master),
            FilteredChannels = component.FilteredChannels
        };
    }

    [AdminCommand(AdminFlags.Fun)]
    private void AddToBandCommand(IConsoleShell shell, string _, string[] args)
    {
        if (!NetEntity.TryParse(args[0], out var firstUidNet) || !TryGetEntity(firstUidNet, out var firstUid))
        {
            shell.WriteError($"Cannot parse first Uid");
            return;
        }

        if (!NetEntity.TryParse(args[1], out var secondUidNet) || !TryGetEntity(secondUidNet, out var secondUid))
        {
            shell.WriteError($"Cannot parse second Uid");
            return;
        }

        if (!HasComp<ActiveInstrumentComponent>(secondUid))
        {
            shell.WriteError($"Puppet instrument is not active!");
            return;
        }

        var otherInstrument = Comp<InstrumentComponent>(secondUid.Value);
        otherInstrument.Playing = true;
        otherInstrument.Master = firstUid;
        Dirty(secondUid.Value, otherInstrument);
    }

    private void OnMidiStart(InstrumentStartMidiEvent msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Uid);

        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        if (args.SenderSession.AttachedEntity != instrument.InstrumentPlayer)
            return;

        instrument.Playing = true;
        Dirty(uid, instrument);
    }

    private void OnMidiStop(InstrumentStopMidiEvent msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Uid);

        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        if (args.SenderSession.AttachedEntity != instrument.InstrumentPlayer)
            return;

        Clean(uid, instrument);
    }

    private void OnMidiSetMaster(InstrumentSetMasterEvent msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Uid);
        var master = GetEntity(msg.Master);

        if (!HasComp<ActiveInstrumentComponent>(uid))
            return;

        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        if (args.SenderSession.AttachedEntity != instrument.InstrumentPlayer)
            return;

        if (master != null)
        {
            if (!HasComp<ActiveInstrumentComponent>(master))
                return;

            if (!TryComp<InstrumentComponent>(master, out var masterInstrument) || masterInstrument.Master != null)
                return;

            instrument.Master = master;
            instrument.FilteredChannels.SetAll(false);
            instrument.Playing = true;
            Dirty(uid, instrument);
            return;
        }

        // Cleanup when disabling master...
        if (master == null && instrument.Master != null)
        {
            Clean(uid, instrument);
        }
    }

    private void OnMidiSetFilteredChannel(InstrumentSetFilteredChannelEvent msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Uid);

        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        if (args.SenderSession.AttachedEntity != instrument.InstrumentPlayer)
            return;

        if (msg.Channel == RobustMidiEvent.PercussionChannel && !instrument.AllowPercussion)
            return;

        instrument.FilteredChannels[msg.Channel] = msg.Value;

        if (msg.Value)
        {
            // Prevent stuck notes when turning off a channel... Shrimple.
            RaiseNetworkEvent(new InstrumentMidiEventEvent(msg.Uid, new []{RobustMidiEvent.AllNotesOff((byte)msg.Channel, 0)}));
        }

        Dirty(uid, instrument);
    }

    private void OnBoundUIClosed(EntityUid uid, InstrumentComponent component, BoundUIClosedEvent args)
    {
        if (HasComp<ActiveInstrumentComponent>(uid)
            && !_bui.IsUiOpen(uid, args.UiKey))
        {
            RemComp<ActiveInstrumentComponent>(uid);
        }

        Clean(uid, component);
    }

    private void OnBoundUIOpened(EntityUid uid, InstrumentComponent component, BoundUIOpenedEvent args)
    {
        EnsureComp<ActiveInstrumentComponent>(uid);
        Clean(uid, component);
    }

    private void OnBoundUIRequestBands(EntityUid uid, InstrumentComponent component, InstrumentBandRequestBuiMessage args)
    {
        foreach (var request in _bandRequestQueue)
        {
            // Prevent spamming requests for the same entity.
            if (request.Entity == args.Entity)
                return;
        }

        _bandRequestQueue.Add(args);
    }

    public (NetEntity, string)[] GetBands(EntityUid uid)
    {
        var metadataQuery = EntityManager.GetEntityQuery<MetaDataComponent>();

        if (Deleted(uid, metadataQuery))
            return Array.Empty<(NetEntity, string)>();

        var list = new ValueList<(NetEntity, string)>();
        var instrumentQuery = EntityManager.GetEntityQuery<InstrumentComponent>();

        if (!TryComp(uid, out InstrumentComponent? originInstrument)
            || originInstrument.InstrumentPlayer is not {} originPlayer)
            return Array.Empty<(NetEntity, string)>();

        // It's probably faster to get all possible active instruments than all entities in range
        var activeEnumerator = EntityManager.EntityQueryEnumerator<ActiveInstrumentComponent>();
        while (activeEnumerator.MoveNext(out var entity, out _))
        {
            if (entity == uid)
                continue;

            // Don't grab puppet instruments.
            if (!instrumentQuery.TryGetComponent(entity, out var instrument) || instrument.Master != null)
                continue;

            // We want to use the instrument player's name.
            if (instrument.InstrumentPlayer is not {} playerUid)
                continue;

            // Maybe a bit expensive but oh well GetBands is queued and has a timer anyway.
            // Make sure the instrument is visible, uses the Opaque collision group so this works across windows etc.
            if (!_interactions.InRangeUnobstructed(uid, entity, MaxInstrumentBandRange,
                    CollisionGroup.Opaque, e => e == playerUid || e == originPlayer))
                continue;

            if (!metadataQuery.TryGetComponent(playerUid, out var playerMetadata)
                || !metadataQuery.TryGetComponent(entity, out var metadata))
                continue;

            list.Add((GetNetEntity(entity), $"{playerMetadata.EntityName} - {metadata.EntityName}"));
        }

        return list.ToArray();
    }

    public void Clean(EntityUid uid, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return;

        if (instrument.Playing)
        {
            var netUid = GetNetEntity(uid);

            // Reset puppet instruments too.
            RaiseNetworkEvent(new InstrumentMidiEventEvent(netUid, new[]{RobustMidiEvent.SystemReset(0)}));

            RaiseNetworkEvent(new InstrumentStopMidiEvent(netUid));
        }

        instrument.Playing = false;
        instrument.Master = null;
        instrument.FilteredChannels.SetAll(false);
        instrument.LastSequencerTick = 0;
        instrument.BatchesDropped = 0;
        instrument.LaggedBatches = 0;
        Dirty(uid, instrument);
    }

    private void OnMidiEventRx(InstrumentMidiEventEvent msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Uid);

        if (!TryComp(uid, out InstrumentComponent? instrument))
            return;

        if (!instrument.Playing
            || args.SenderSession.AttachedEntity != instrument.InstrumentPlayer
            || instrument.InstrumentPlayer == null
            || args.SenderSession.AttachedEntity is not { } attached)
        {
            return;
        }

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
                    _popup.PopupEntity(Loc.GetString("instrument-component-finger-cramps-light-message"),
                        uid, attached, PopupType.SmallCaution);
                }
                else if (instrument.LaggedBatches == (int) (MaxMidiLaggedBatches * (2 / 3d) + 1))
                {
                    _popup.PopupEntity(Loc.GetString("instrument-component-finger-cramps-serious-message"),
                        uid, attached, PopupType.MediumCaution);
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

        instrument.LastSequencerTick = Math.Max(maxTick, minTick);

        if (send || !instrument.RespectMidiLimits)
        {
            RaiseNetworkEvent(msg);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_bandRequestQueue.Count > 0 && _bandRequestTimer < _timing.RealTime)
        {
            _bandRequestTimer = _timing.RealTime.Add(TimeSpan.FromSeconds(BandRequestDelay));

            foreach (var request in _bandRequestQueue)
            {
                var entity = GetEntity(request.Entity);

                var nearby = GetBands(entity);
                _bui.ServerSendUiMessage(entity, request.UiKey, new InstrumentBandResponseBuiMessage(nearby), request.Actor);
            }

            _bandRequestQueue.Clear();
        }

        var activeQuery = EntityManager.GetEntityQuery<ActiveInstrumentComponent>();
        var metadataQuery = EntityManager.GetEntityQuery<MetaDataComponent>();
        var transformQuery = EntityManager.GetEntityQuery<TransformComponent>();

        var query = AllEntityQuery<ActiveInstrumentComponent, InstrumentComponent>();
        while (query.MoveNext(out var uid, out _, out var instrument))
        {
            if (instrument.Master is {} master)
            {
                if (Deleted(master, metadataQuery))
                {
                    Clean(uid, instrument);
                }

                var masterActive = activeQuery.CompOrNull(master);
                if (masterActive == null)
                {
                    Clean(uid, instrument);
                }

                var trans = transformQuery.GetComponent(uid);
                var masterTrans = transformQuery.GetComponent(master);
                if (!masterTrans.Coordinates.InRange(EntityManager, _transform, trans.Coordinates, 10f))
                {
                    Clean(uid, instrument);
                }
            }

            if (instrument.RespectMidiLimits &&
                (instrument.BatchesDropped >= MaxMidiBatchesDropped
                 || instrument.LaggedBatches >= MaxMidiLaggedBatches))
            {
                if (instrument.InstrumentPlayer is {Valid: true} mob)
                {
                    _stuns.TryParalyze(mob, TimeSpan.FromSeconds(1), true);

                    _popup.PopupEntity(Loc.GetString("instrument-component-finger-cramps-max-message"),
                        uid, mob, PopupType.LargeCaution);
                }

                // Just in case
                Clean(uid);
                _bui.CloseUi(uid, InstrumentUiKey.Key);
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

    public void ToggleInstrumentUi(EntityUid uid, EntityUid actor, InstrumentComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _bui.TryToggleUi(uid, InstrumentUiKey.Key, actor);
    }

    public override bool ResolveInstrument(EntityUid uid, ref SharedInstrumentComponent? component)
    {
        if (component is not null)
            return true;

        TryComp<InstrumentComponent>(uid, out var localComp);
        component = localComp;
        return component != null;
    }
}
