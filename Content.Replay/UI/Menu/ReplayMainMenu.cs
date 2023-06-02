using System.Linq;
using Content.Client.Message;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Robust.Client;
using Robust.Client.Replays.Loading;
using Robust.Client.ResourceManagement;
using Robust.Client.Serialization;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using static Robust.Shared.Replays.IReplayRecordingManager;

namespace Content.Replay.UI.Menu;

/// <summary>
///     Main menu screen for selecting and loading replays.
/// </summary>
public sealed class ReplayMainScreen : State
{
    [Dependency] private readonly IResourceManager _resMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IReplayLoadManager _loadMan = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameController _controllerProxy = default!;
    [Dependency] private readonly IClientRobustSerializer _serializer = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    private ReplayMainMenuControl _mainMenuControl = default!;
    private SelectReplayWindow? _selectWindow;

    // TODO cvar (or add a open-file dialog?).
    public static readonly ResPath DefaultReplayDirectory = new("/replays");
    private readonly ResPath _directory = DefaultReplayDirectory;

    private List<ResPath> _replays = new();
    private ResPath? _selected;

    // Should probably be localized, but should never happen, so...
    public const string Error = "Error";

    protected override void Startup()
    {
        _mainMenuControl = new(_resourceCache);
        _userInterfaceManager.StateRoot.AddChild(_mainMenuControl);

        _mainMenuControl.SelectButton.OnPressed += OnSelectPressed;
        _mainMenuControl.QuitButton.OnPressed += QuitButtonPressed;
        _mainMenuControl.OptionsButton.OnPressed += OptionsButtonPressed;
        _mainMenuControl.FolderButton.OnPressed += OnFolderPressed;
        _mainMenuControl.LoadButton.OnPressed += OnLoadpressed;

        RefreshReplays();
        SelectReplay(_replays.FirstOrNull());
        if (_selected == null) // force initial update
            UpdateSelectedInfo();
    }

    /// <summary>
    ///     Read replay meta-data and update the replay info box.
    /// </summary>
    private void UpdateSelectedInfo()
    {
        var info = _mainMenuControl.Info;

        if (_selected is not { } replay)
        {
            info.SetMarkup(Loc.GetString("replay-info-none-selected"));
            info.HorizontalAlignment = Control.HAlignment.Center;
            info.VerticalAlignment = Control.VAlignment.Center;
            _mainMenuControl.LoadButton.Disabled = true;
            return;
        }

        if (!_resMan.UserData.Exists(replay)
            || _loadMan.LoadYamlMetadata(_resMan.UserData, replay) is not { } data)
        {
            info.SetMarkup(Loc.GetString("replay-info-invalid"));
            info.HorizontalAlignment = Control.HAlignment.Center;
            info.VerticalAlignment = Control.VAlignment.Center;
            _mainMenuControl.LoadButton.Disabled = true;
            return;
        }

        var file = replay.ToRelativePath().ToString();
        data.TryGet<ValueDataNode>(Time, out var timeNode);
        data.TryGet<ValueDataNode>(Duration, out var durationNode);
        data.TryGet<ValueDataNode>("roundId", out var roundIdNode);
        data.TryGet<ValueDataNode>(Hash, out var hashNode);
        DateTime.TryParse(timeNode?.Value, out var time);
        TimeSpan.TryParse(durationNode?.Value, out var duration);

        var forkId = string.Empty;
        if (data.TryGet<ValueDataNode>(Fork, out var forkNode))
        {
            // TODO REPLAYS somehow distribute and load from build.json?
            var clientFork = _cfg.GetCVar(CVars.BuildForkId);
            if (string.IsNullOrWhiteSpace(clientFork))
                forkId = forkNode.Value;
            else if (forkNode.Value == clientFork)
                forkId = $"[color=green]{forkNode.Value}[/color]";
            else
                forkId = $"[color=yellow]{forkNode.Value}[/color]";
        }

        var forkVersion = string.Empty;
        if (data.TryGet<ValueDataNode>(ForkVersion, out var versionNode))
        {
            forkVersion = versionNode.Value;
            // Why does this not have a try-convert function? I just want to check if it looks like a hash code.
            try
            {
                Convert.FromHexString(forkVersion);
                // version is a probably some GH hash. Crop it to keep the info box small.
                forkVersion = forkVersion[..16];
            }
            catch
            {
                // ignored
            }

            // TODO REPLAYS somehow distribute and load from build.json?
            var clientVer = _cfg.GetCVar(CVars.BuildVersion);
            if (!string.IsNullOrWhiteSpace(clientVer))
            {
                if (versionNode.Value == clientVer)
                    forkVersion = $"[color=green]{forkVersion}[/color]";
                else
                    forkVersion = $"[color=yellow]{forkVersion}[/color]";
            }
        }


        var typeHash = hashNode?.Value ?? Error;
        if (Convert.FromHexString(typeHash).SequenceEqual(_serializer.GetSerializableTypesHash()))
        {
            typeHash = $"[color=green]{typeHash[..16]}[/color]";
            _mainMenuControl.LoadButton.Disabled = false;
        }
        else
        {
            typeHash = $"[color=red]{typeHash[..16]}[/color]";
            _mainMenuControl.LoadButton.Disabled = true;
        }

        var engineVersion = string.Empty;
        if (data.TryGet<ValueDataNode>(Engine, out var engineNode))
        {
            var clientVer = _cfg.GetCVar(CVars.BuildEngineVersion);
            if (string.IsNullOrWhiteSpace(clientVer))
                engineVersion = engineNode.Value;
            else if (engineNode.Value == clientVer)
                engineVersion = $"[color=green]{engineNode.Value}[/color]";
            else
                engineVersion = $"[color=yellow]{engineNode.Value}[/color]";
        }

        // Strip milliseconds.
        // why the fuck isn't there a general format string that suppresses milliseconds.
        duration = new((int)Math.Floor(duration.TotalDays), duration.Hours, duration.Minutes, duration.Seconds);

        info.HorizontalAlignment = Control.HAlignment.Left;
        info.VerticalAlignment = Control.VAlignment.Top;
        info.SetMarkup(Loc.GetString(
            "replay-info-info",
            ("file", file),
            ("time", time),
            ("roundId", roundIdNode?.Value ?? Error),
            ("duration", duration),
            ("forkId", forkNode?.Value ?? Error),
            ("version", forkVersion),
            ("engVersion", engineVersion),
            ("hash", typeHash)));
    }

    private void OnFolderPressed(BaseButton.ButtonEventArgs obj)
    {
        _resMan.UserData.CreateDir(_directory);
        _resMan.UserData.OpenOsWindow(_directory);
    }

    private void OnLoadpressed(BaseButton.ButtonEventArgs obj)
    {
        if (_selected.HasValue)
            _loadMan.LoadAndStartReplay(_resMan.UserData, _selected.Value);
    }

    private void RefreshReplays()
    {
        _replays.Clear();

        foreach (var entry in _resMan.UserData.DirectoryEntries(_directory))
        {
            var file = _directory / entry;
            if (_resMan.UserData.Exists(file / MetaFile))
                _replays.Add(file);
        }

        _selectWindow?.Repopulate(_replays);

        if (_selected.HasValue && !_replays.Contains(_selected.Value))
            SelectReplay(null);
        else
            _selectWindow?.UpdateSelected(_selected);
    }

    public void SelectReplay(ResPath? replay)
    {
        if (_selected == replay)
            return;

        _selected = replay;
        UpdateSelectedInfo();
        _selectWindow?.UpdateSelected(replay);
    }

    protected override void Shutdown()
    {
        _mainMenuControl.Dispose();
        _selectWindow?.Dispose();
    }

    private void OptionsButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _userInterfaceManager.GetUIController<OptionsUIController>().ToggleWindow();
    }

    private void QuitButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _controllerProxy.Shutdown();
    }

    private void OnSelectPressed(BaseButton.ButtonEventArgs args)
    {
        RefreshReplays();
        _selectWindow ??= new(this);
        _selectWindow.Repopulate(_replays);
        _selectWindow.UpdateSelected(_selected);
        _selectWindow.OpenCentered();
    }
}
