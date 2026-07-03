using Content.Client.Interactable;
using Content.Shared.ActionBlocker;
using Content.Shared.Instruments;
using Content.Shared.Instruments.UI;
using Robust.Client.Audio.Midi;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client.Instruments.UI;

public sealed partial class InstrumentBoundUserInterface : BoundUserInterface
{
    private const int MaxSearchDepth = 16;
    private const string SawmillCategory = "instrumentui";

    [Dependency] private IMidiManager _midiManager = default!;
    [Dependency] private ILocalizationManager _loc = default!;
    [Dependency] private ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    private InstrumentSystem _instruments = default!;
    private ActionBlockerSystem _actionBlockerSystem = default!;
    private InteractionSystem _interactionSystem = default!;
    private SharedContainerSystem _sharedContainerSystem = default!;

    private readonly FileMidiSource _fileSource = new();
    private readonly BandMidiSource _bandSource = new();
    private readonly InputMidiSource _inputSource = new();

    private readonly ChannelsControl _channelsControl = new();

    private InstrumentMenu? _instrumentMenu;

    public InstrumentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _logManager.GetSawmill(SawmillCategory);
        _instruments = EntMan.System<InstrumentSystem>();
        _actionBlockerSystem = EntMan.System<ActionBlockerSystem>();
        _interactionSystem = EntMan.System<InteractionSystem>();
        _sharedContainerSystem = EntMan.System<SharedContainerSystem>();
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
            return;

        instrument.OnMidiPlaybackEnded += OnMidiPlaybackEnded;

        _instruments.OnChannelsUpdated += OnChannelsUpdated;

        _fileSource.StartPlayingRequest += OnStartPlayingRequest;
        _fileSource.StopPlayingRequest += OnStopPlayingRequest;
        _fileSource.LoopingToggled += OnLoopToggledRequest;
        _fileSource.TrackPositionChangeRequest += OnTrackPositionChangeRequest;
        _fileSource.SetEntity(Owner);

        _bandSource.RefreshBandRequest += OnRefreshBandsRequest;
        _bandSource.JoinBandRequest += OnSetBandMasterRequest;

        _inputSource.OpenInputRequest += OnOpenInputRequest;
        _inputSource.CloseInputRequest += OnCloseInputRequest;

        _channelsControl.ChannelsUpdateRequest += OnChannelsUpdateRequest;
        _channelsControl.SwitchFilteredChannel += OnSwitchFilteredChannel;

        _instrumentMenu = this.CreateWindow<InstrumentMenu>();

        if(EntMan.TryGetComponent<MetaDataComponent>(Owner, out var metaData))
            _instrumentMenu.Title = metaData.EntityName;

        _instrumentMenu.SetupSources(_fileSource, _bandSource, _inputSource);
        _instrumentMenu.SetMidiAvailability(_midiManager.IsAvailable);
        _instrumentMenu.SwitchMode(_fileSource);
        _instrumentMenu.SetInstrument((Owner, instrument));

        _instrumentMenu.AddConfigurationControl(
            _loc.GetString("instruments-component-menu-channels-label"),
            _channelsControl);

        // Append additional configuration controls here
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not InstrumentBandResponseBuiMessage bandRx)
            return;

        var entities = new List<(EntityUid, string)>();
        foreach (var netEnt in bandRx.Nearby)
        {
            entities.Add((EntMan.GetEntity(netEnt.Item1), netEnt.Item2));
        }

        _bandSource.Populate(entities);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _fileSource.StartPlayingRequest -= OnStartPlayingRequest;
        _fileSource.StopPlayingRequest -= OnStopPlayingRequest;
        _fileSource.LoopingToggled -= OnLoopToggledRequest;
        _fileSource.TrackPositionChangeRequest -= OnTrackPositionChangeRequest;
        _fileSource.SetEntity(Owner);

        _bandSource.RefreshBandRequest -= OnRefreshBandsRequest;
        _bandSource.JoinBandRequest -= OnSetBandMasterRequest;

        _inputSource.OpenInputRequest -= OnOpenInputRequest;
        _inputSource.CloseInputRequest -= OnCloseInputRequest;

        if (EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
        {
            instrument.OnMidiPlaybackEnded -= OnMidiPlaybackEnded;
        }
    }

    private void OnSwitchFilteredChannel(int channelIndex, bool state)
    {
        _instruments.SetFilteredChannel(Owner, channelIndex, state);
    }

    private void OnChannelsUpdateRequest()
    {
        UpdateChannels();
    }

    private void OnChannelsUpdated()
    {
        UpdateChannels();
    }

    private void OnMidiPlaybackEnded()
    {
        _fileSource.SelectNextTrack();
    }

    private void OnSetBandMasterRequest(EntityUid ent)
    {
        if (!PlayCheck())
            return;

        _instruments.SetMaster(Owner, ent);
    }

    private void OnRefreshBandsRequest()
    {
        SendMessage(new InstrumentBandRequestBuiMessage());
    }

    private void OnLoopToggledRequest(bool toggled)
    {
        if (EntMan.TryGetComponent(Owner, out InstrumentComponent? instrumentComp))
        {
            instrumentComp.LoopMidi = toggled;
        }

        _instruments.UpdateRenderer(Owner);
    }

    private void OnTrackPositionChangeRequest(int value)
    {
        _instruments.SetPlayerTick(Owner, value);
    }

    private void OnOpenInputRequest()
    {
        if (!PlayCheck())
            return;

        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        _instruments.OpenInput(Owner, instrument);
    }

    private void OnCloseInputRequest()
    {
        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        _instruments.CloseInput(Owner, false, instrument);
    }

    private void OnStopPlayingRequest()
    {
        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        _instruments.CloseMidi(Owner, false, instrument);
    }

    private void OnStartPlayingRequest(byte[] trackData)
    {
        try
        {
            if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
                return;

            // Close any song that is already playing.
            if (instrument.IsMidiOpen)
                _instruments.CloseMidi(Owner, false, instrument);

            if (!_fileSource.IsPlaying)
                return;

            if (!PlayCheck())
                return;

            if (!_instruments.OpenMidi(Owner, trackData, instrument))
                _fileSource.IsPlaying = false;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to play next midi track: {e.Message}");
            _fileSource.IsPlaying = false;
        }
    }

    private bool PlayCheck()
    {
        if (!EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
            return false;

        var localEntity = PlayerManager.LocalEntity;

        // If we don't have a player or controlled entity, we return.
        if (localEntity == null)
            return false;

        // By default, allow an instrument to play itself and skip all other checks
        if (localEntity == Owner)
            return true;

        // If we're a handheld instrument, we might be in a container. Get it just in case.
        _sharedContainerSystem.TryGetContainingContainer((Owner, null, null), out var conMan);

        // If the instrument is handheld, and we're not holding it, we return.
        if (instrument.Handheld && (conMan == null || conMan.Owner != localEntity))
            return false;

        if (!_actionBlockerSystem.CanInteract(localEntity.Value, Owner))
            return false;

        if (!_interactionSystem.InRangeUnobstructed(localEntity.Value, Owner))
            return false;

        return true;
    }

    /// <summary>
    /// Walks up the tree of instrument masters to find the truest master of them all.
    /// </summary>
    private ActiveInstrumentComponent? ResolveActiveInstrument(InstrumentComponent comp)
    {
        var instrument = new Entity<InstrumentComponent>(Owner, comp);

        for(var i = 0; i < MaxSearchDepth; i++)
        {
            if (instrument.Comp.Master is not { } master)
                break;

            if(!EntMan.TryGetComponent<InstrumentComponent>(master, out var masterComp))
                break;

            instrument = new Entity<InstrumentComponent>(master, masterComp);
        }

        return EntMan.GetComponentOrNull<ActiveInstrumentComponent>(instrument.Owner);
    }

    private void UpdateChannels()
    {
        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        // Ignore channel switch request while updating.
        _channelsControl.SwitchFilteredChannel -= OnSwitchFilteredChannel;
        List<(int, string, bool)> channelSettings = [];

        var activeInstrument = ResolveActiveInstrument(instrument);

        for (var i = 0; i < RobustMidiEvent.MaxChannels; i++)
        {
            var label = _loc.GetString("instrument-component-channel-name",
                ("number", i));
            if (activeInstrument != null
                && activeInstrument.Tracks.TryGetValue(i, out var resolvedMidiChannel)
                && resolvedMidiChannel != null)
            {
                if (_channelsControl.DisplayTrackNames)
                {
                    label = resolvedMidiChannel switch
                    {
                        { TrackName: not null, InstrumentName: not null } =>
                            Loc.GetString("instruments-component-channels-multi",
                                ("channel", i),
                                ("name", resolvedMidiChannel.TrackName),
                                ("other", resolvedMidiChannel.InstrumentName)),
                        { TrackName: not null } =>
                            Loc.GetString("instruments-component-channels-single",
                                ("channel", i),
                                ("name", resolvedMidiChannel.TrackName)),
                        _ => label,
                    };
                }
                else
                {
                    label = resolvedMidiChannel switch
                    {
                        { ProgramName: not null } =>
                            Loc.GetString("instruments-component-channels-single",
                                ("channel", i),
                                ("name", resolvedMidiChannel.ProgramName)),
                        _ => label,
                    };
                }
            }

            var state = !instrument?.FilteredChannels[i] ?? false;
            channelSettings.Add((i, label, state));
        }

        _channelsControl.SetChannels(channelSettings);
        _channelsControl.SwitchFilteredChannel += OnSwitchFilteredChannel;
    }
}
