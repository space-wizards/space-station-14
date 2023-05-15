using System.Linq;
using Content.Client.Message;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Robust.Client;
using Robust.Client.ResourceManagement;
using Robust.Client.Serialization;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Replay.UI.Menu;

// TODO REPLAYS maybe just remove this?
// The necessity for this screen depends entirely on how replays end up being launched.
// If its via an un-sandboxed standalone exe, this should just use a file selection dialog
// If its a sandboxed exe, I guess the current %appdata% dropdown is fine?
// If its via the launcher somehow.. uhhh.... I guess this isn't needed outside of debug?
/// <summary>
///     Main menu screen that is the first screen to be displayed when the game starts.
/// </summary>
public sealed class ReplayMainScreen : State
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IResourceManager _resourceMan = default!;
    [Dependency] private readonly Manager.ReplayManager _replayMan = default!;
    [Dependency] private readonly IGameController _controllerProxy = default!;
    [Dependency] private readonly IClientRobustSerializer _serializer = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    private ReplayMainMenuControl _mainMenuControl = default!;

    // TODO cvar (or add a open-file dialog?).
    public static readonly ResPath DefaultReplayDirectory = new("/replays");

    private IWritableDirProvider? _directory;

    protected override void Startup()
    {
        _mainMenuControl = new(_resourceCache);
        _userInterfaceManager.StateRoot.AddChild(_mainMenuControl);
        _mainMenuControl.ReplaySelect.OnItemSelected += OnItemSelected;
        _mainMenuControl.QuitButton.OnPressed += QuitButtonPressed;
        _mainMenuControl.OptionsButton.OnPressed += OptionsButtonPressed;
        _mainMenuControl.RefreshButton.OnPressed += OnRefreshPressed;
        _mainMenuControl.LoadButton.OnPressed += OnLoadpressed;

        _directory ??= _resourceMan.UserData.OpenSubdirectory(DefaultReplayDirectory);
        RefreshReplays();
    }

    private void OnItemSelected(OptionButton.ItemSelectedEventArgs obj)
    {
        RefreshSelection();
    }

    /// <summary>
    ///     Read replay meta-data and update the replay info box.
    /// </summary>
    private void RefreshSelection()
    {
        var info = _mainMenuControl.Info;

        if (_directory == null
            || _mainMenuControl.ReplaySelect.SelectedMetadata is not ResPath path
            || !_directory.Exists(path)
            || _replayMan.LoadYamlMetadata(_directory.OpenSubdirectory(path)) is not { } data)
        {
            info.SetMarkup(Loc.GetString("replay-info-none"));
            info.HorizontalAlignment = Control.HAlignment.Center;
            info.VerticalAlignment = Control.VAlignment.Center;
            _mainMenuControl.LoadButton.Disabled = true;
            return;
        }

        var file = path.ToRelativePath().ToString();
        var time = DateTime.Parse(((ValueDataNode) data["time"]).Value);
        var duration = TimeSpan.Parse(((ValueDataNode) data["duration"]).Value);
        var engVersion = ((ValueDataNode) data["engineVersion"]).Value;
        var forkId = ((ValueDataNode) data["buildForkId"]).Value;
        var forkVersion = ((ValueDataNode) data["buildVersion"]).Value;

        // Why does this not have a try-convert function???
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

        var typeHash = ((ValueDataNode) data["typeHash"]).Value;
        if (Convert.FromHexString(typeHash).SequenceEqual(_serializer.GetSerializableTypesHash()))
            typeHash = $"[color=green]{typeHash[..16]}[/color]";
        else
            typeHash = $"[color=red]{typeHash[..16]}[/color]";

        // TODO REPLAYS
        // Include engine, forkId, & forkVersion data with the client.
        // Currently these are server-exclusive CVARS.
        // If they differ from the current file, highlight the text in yellow (file may be loadable, but may cause issues).

        info.HorizontalAlignment = Control.HAlignment.Left;
        info.VerticalAlignment = Control.VAlignment.Top;
        info.SetMarkup(Loc.GetString(
            "replay-info-info",
            ("file", file),
            ("time", time),
            ("duration", duration),
            ("forkId", forkId),
            ("version", forkVersion),
            ("engVersion", engVersion),
            ("hash", typeHash)));

        _mainMenuControl.LoadButton.Disabled = false;
    }

    private void OnRefreshPressed(BaseButton.ButtonEventArgs obj)
    {
        RefreshReplays();
    }

    private void OnLoadpressed(BaseButton.ButtonEventArgs obj)
    {
        if (_directory == null)
            return;

        if (_mainMenuControl.ReplaySelect.SelectedMetadata is not ResPath path)
            return;

        var dir = _directory.OpenSubdirectory(path);
        _replayMan.LoadReplay(dir);
    }

    private void RefreshReplays()
    {
        _mainMenuControl.ReplaySelect.Clear();

        var i = 0;
        if (_directory != null)
        {
            foreach (var file in _directory.DirectoryEntries(ResPath.Root))
            {
                var path = new ResPath(file).ToRootedPath();
                if (!_directory.Exists(path / Manager.ReplayManager.YamlFilename))
                    continue;

                _mainMenuControl.ReplaySelect.AddItem(file, i);
                _mainMenuControl.ReplaySelect.SetItemMetadata(_mainMenuControl.ReplaySelect.GetIdx(i), path);
                i++;
            }
        }

        if (i == 0)
        {
            _mainMenuControl.ReplaySelect.Disabled = true;
            _mainMenuControl.ReplaySelect.AddItem(Loc.GetString("replay-menu-none"));
        }
        else
        {
            _mainMenuControl.ReplaySelect.Disabled = false;
            _mainMenuControl.ReplaySelect.Select(0);
        }
        RefreshSelection();
    }

    /// <inheritdoc />
    protected override void Shutdown()
    {
        _mainMenuControl.Dispose();
    }

    private void OptionsButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _userInterfaceManager.GetUIController<OptionsUIController>().ToggleWindow();
    }

    private void QuitButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _controllerProxy.Shutdown();
    }
}
