using System.IO;
using System.Threading.Tasks;
using Content.Client.Interactable;
using Content.Client.UserInterface.Controls;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Instruments;
using Content.Shared.Instruments.UI;
using Robust.Client.Audio.Midi;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Instruments.UI;

public sealed class InstrumentBoundUserInterface : BoundUserInterface
{
    private static readonly ResPath UserMidiDirectory = new("/UserMidis/");

    [Dependency] private readonly IMidiManager _midiManager = default!;
    [Dependency] private readonly IFileDialogManager _dialogs = default!;
    [Dependency] private readonly IResourceManager _resManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private readonly InstrumentSystem _instruments;

    private readonly FileMidiSource _fileSource = new();
    private readonly BandMidiSource _bandSource = new();
    private readonly InputMidiSource _inputSource = new();

    private readonly ChannelsControl _channelsControl = new();

    private bool _isMidiFileDialogueWindowOpen;
    private DialogWindow? _reasonDialog;
    private InstrumentMenu? _instrumentMenu;

    public InstrumentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _instruments = EntMan.System<InstrumentSystem>();
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
            return;

        LoadStoredUserMidis();

        instrument.OnMidiPlaybackEnded += OnMidiPlaybackEnded;

        _instruments.OnChannelsUpdated += OnChannelsUpdated;

        _fileSource.StartPlayingRequest += OnStartPlayingRequest;
        _fileSource.StopPlayingRequest += OnStopPlayingRequest;
        _fileSource.LoopingToggled += OnLoopToggledRequest;
        _fileSource.TrackPositionChangeRequest += OnTrackPositionChangeRequest;
        _fileSource.FileAddNewRequest += OnFileAddNewRequest;
        _fileSource.FileRenameRequest += OnFileRenameRequest;
        _fileSource.FileRemoveRequest += OnFileRemoveRequest;
        _fileSource.SetEntity(Owner);

        _bandSource.RefreshBandRequest += OnRefreshBandsRequest;
        _bandSource.JoinBandRequest += OnSetBandMasterRequest;

        _inputSource.OpenInputRequest += OnOpenInputRequest;
        _inputSource.CloseInputRequest += OnCloseInputRequest;

        _channelsControl.ChannelsUpdateRequest += OnChannelsUpdateRequest;
        _channelsControl.SwitchFilteredChannel += OnSwitchFilteredChannel;

        _instrumentMenu = this.CreateWindow<InstrumentMenu>();
        _instrumentMenu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
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
        _fileSource.FileAddNewRequest -= OnFileAddNewRequest;
        _fileSource.FileRenameRequest -= OnFileRenameRequest;
        _fileSource.FileRemoveRequest -= OnFileRemoveRequest;
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
        // Give the InstrumentSystem time to clear the renderer, preventing it from reusing the renderer it's about to dispose.
        Timer.Spawn(1000, () => { _fileSource.PlayNextTrack(); });
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
        var instrument = EntMan.System<InstrumentSystem>();

        if (EntMan.TryGetComponent(Owner, out InstrumentComponent? instrumentComp))
        {
            instrumentComp.LoopMidi = toggled;
        }

        instrument.UpdateRenderer(Owner);
    }

    private void OnTrackPositionChangeRequest(int value)
    {
        EntMan.System<InstrumentSystem>().SetPlayerTick(Owner, value);
    }

    private void OnOpenInputRequest()
    {
        if (!PlayCheck())
            return;

        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        EntMan.System<InstrumentSystem>().OpenInput(Owner, instrument);
    }

    private void OnCloseInputRequest()
    {
        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        EntMan.System<InstrumentSystem>().CloseInput(Owner, false, instrument);
    }

    private void OnStopPlayingRequest()
    {
        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        EntMan.System<InstrumentSystem>().CloseMidi(Owner, false, instrument);
    }

    private void OnStartPlayingRequest(byte[] trackData)
    {
        if (!PlayCheck())
            return;

        if (!EntMan.TryGetComponent<InstrumentComponent>(Owner, out var instrument))
            return;

        // Close any song that is already playing.
        if (instrument.IsMidiOpen)
            EntMan.System<InstrumentSystem>().CloseMidi(Owner, false, instrument);

        Timer.Spawn(1000,
            () =>
            {
                if (!_fileSource.IsPlaying)
                    return;

                if (!PlayCheck())
                    return;

                if (!EntMan.System<InstrumentSystem>().OpenMidi(Owner, trackData, instrument))
                    _fileSource.IsPlaying = false;
            });
    }

    private async void OnFileAddNewRequest()
    {
        try
        {
            if (_isMidiFileDialogueWindowOpen)
                return;

            var filters = new FileDialogFilters(new FileDialogFilters.Group("mid", "midi"));

            // TODO: Once the file dialogue manager can handle focusing or closing windows, improve this logic to close
            // or focus the previously-opened window.
            _isMidiFileDialogueWindowOpen = true;

            await using var file = await _dialogs.OpenFile(filters, FileAccess.Read);

            _isMidiFileDialogueWindowOpen = false;

            // did the instrument menu get closed while waiting for the user to select a file?
            if (!IsOpened)
                return;

            if (file == null)
                return;

            var fileName = DateTime.Now.Ticks + ".midi";
            var data = file.CopyToArray();
            StoreMidiFile(fileName, data);
            _fileSource.AddTrack(fileName, data);
        }
        catch
        {
            _userInterfaceManager.Popup(Loc.GetString("instruments-component-menu-files-error"));
        }
    }

    private void OnFileRemoveRequest(string name)
    {
        try
        {
            var path = new ResPath(UserMidiDirectory + name).Clean();
            _resManager.UserData.Delete(path);
            _fileSource.RemoveTrack(name);
        }
        catch
        {
            // ignored
        }
    }

    private void OnFileRenameRequest(string originalName)
    {
        if (_reasonDialog != null)
        {
            _reasonDialog.MoveToFront();
            return;
        }

        if (originalName.Length == 0)
            return;

        const string field = "name";
        var title = Loc.GetString("instruments-component-menu-files-rename-dialog-title");
        var prompt = Loc.GetString("instruments-component-menu-files-rename-dialog-prompt");
        var entry = new QuickDialogEntry(field, QuickDialogEntryType.ShortText, prompt, originalName);
        var entries = new List<QuickDialogEntry> { entry };
        _reasonDialog = new DialogWindow(title, entries);

        _reasonDialog.OnConfirmed += responses =>
        {
            var newName = responses[field];
            if (newName.Length < 1)
                return;
            if (!newName.EndsWith(".midi") && !newName.EndsWith(".mid"))
                newName += ".midi";
            if (RenameMidiFile(originalName, newName))
                _fileSource.UpdateTrackName(originalName, newName);
        };

        _reasonDialog.OnClose += () => { _reasonDialog = null; };
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

        var container = EntMan.System<SharedContainerSystem>();
        // If we're a handheld instrument, we might be in a container. Get it just in case.
        container.TryGetContainingContainer((Owner, null, null), out var conMan);

        // If the instrument is handheld, and we're not holding it, we return.
        if (instrument.Handheld && (conMan == null || conMan.Owner != localEntity))
            return false;

        if (!EntMan.System<ActionBlockerSystem>().CanInteract(localEntity.Value, Owner))
            return false;

        if (!EntMan.System<InteractionSystem>().InRangeUnobstructed(localEntity.Value, Owner))
            return false;

        return true;
    }

    /// <summary>
    /// Walks up the tree of instrument masters to find the truest master of them all.
    /// </summary>
    private ActiveInstrumentComponent ResolveActiveInstrument(InstrumentComponent? comp)
    {
        comp ??= EntMan.GetComponent<InstrumentComponent>(Owner);

        var instrument = new Entity<InstrumentComponent>(Owner, comp);

        while (true)
        {
            if (instrument.Comp.Master == null)
                break;

            instrument = new Entity<InstrumentComponent>((EntityUid)instrument.Comp.Master,
                EntMan.GetComponent<InstrumentComponent>((EntityUid)instrument.Comp.Master));
        }

        return EntMan.GetComponent<ActiveInstrumentComponent>(instrument.Owner);
    }

    private void UpdateChannels()
    {
        // Ignore channel switch request while updating.
        _channelsControl.SwitchFilteredChannel -= OnSwitchFilteredChannel;
        List<(int, string, bool)> channelSettings = [];
        var instrument = EntMan.GetComponent<InstrumentComponent>(Owner);
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

    private void EnsureMidiDirectoryExists()
    {
        if (!_resManager.UserData.Exists(UserMidiDirectory))
            _resManager.UserData.CreateDir(UserMidiDirectory);
    }

    private void StoreMidiFile(string filename, byte[] data)
    {
        EnsureMidiDirectoryExists();
        _resManager.UserData.WriteAllBytes(new ResPath(UserMidiDirectory + filename), data);
    }

    private bool RenameMidiFile(string oldName, string newName)
    {
        try
        {
            EnsureMidiDirectoryExists();
            var oldPath = new ResPath(UserMidiDirectory + oldName);
            var newPath = new ResPath(UserMidiDirectory + newName);
            oldPath = oldPath.Clean();
            newPath = newPath.Clean();
            _resManager.UserData.Rename(oldPath, newPath);
            return true;
        }
        catch
        {
            _userInterfaceManager.Popup(Loc.GetString("instruments-component-menu-files-error"));
            return false;
        }
    }

    private async void LoadStoredUserMidis()
    {
        if (!_resManager.UserData.IsDir(UserMidiDirectory))
            return;

        // Using await because large user libraries might take a few
        // seconds to load, especially on slower machines.
        _fileSource.PopulateTrackList(await Task.Run(() => LoadMidisFromDirectory(UserMidiDirectory)));
    }

    private List<(string, byte[])> LoadMidisFromDirectory(ResPath directory)
    {
        List<(string, byte[])> tracks = [];
        foreach (var path in _resManager.UserData.DirectoryEntries(directory))
        {
            try
            {
                var filePath = new ResPath(UserMidiDirectory + path);
                if (!filePath.Extension.Equals("midi") && !filePath.Extension.Equals("mid"))
                    continue;

                var data = _resManager.UserData.ReadAllBytes(filePath);
                tracks.Add((filePath.Filename, data));
            }
            catch
            {
                // ignored
            }
        }

        return tracks;
    }
}
