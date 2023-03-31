using Content.Client.UserInterface.Systems.EscapeMenu;
using Robust.Client;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Replay.UI.Menu;

// TODO REPLAYS maybe just remove this?
// The neccesity for this screen depends entirely on how replays end up being launched.
// If its via an un-sandboxed standalone exe, this should just use a file selection dialog
// If its a sandboxed exe, I guess the current %appdata% dropdown is fine?
// If its via the launcher somehow.. uhhh.... I guess this isn't needed outside of debug?
// Also:
// TODO REPLAYS
// localize button text.
/// <summary>
///     Main menu screen that is the first screen to be displayed when the game starts.
/// </summary>
public sealed class ReplayMainScreen : State
{
    [Dependency] private readonly IGameController _controllerProxy = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IResourceManager _resourceMan = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly Manager.ReplayManager _replayMan = default!;

    private ReplayMainMenuControl _mainMenuControl = default!;

    public static readonly ResourcePath DefaultReplayDirectory = new("/replays");

    private IWritableDirProvider? _directory;

    protected override void Startup()
    {
        _mainMenuControl = new(_resourceCache);
        _userInterfaceManager.StateRoot.AddChild(_mainMenuControl);
        _mainMenuControl.QuitButton.OnPressed += QuitButtonPressed;
        _mainMenuControl.OptionsButton.OnPressed += OptionsButtonPressed;
        _mainMenuControl.RefreshButton.OnPressed += OnRefreshPressed;
        _mainMenuControl.LoadButton.OnPressed += OnLoadpressed;

        // TODO REPLAYS why is this try catch here again?
        // This seems very fishy
        try
        {
            _directory ??= _resourceMan.UserData.OpenSubdirectory(DefaultReplayDirectory);
            RefreshReplays();
        }
        catch
        {
        }
    }

    public void SetDirectory(IWritableDirProvider? directory)
    {
        _directory = directory;
        RefreshReplays();
    }

    private void OnRefreshPressed(BaseButton.ButtonEventArgs obj)
    {
        RefreshReplays();
    }

    private void OnLoadpressed(BaseButton.ButtonEventArgs obj)
    {
        if (_directory == null)
            return;

        if (_mainMenuControl.ReplaySelect?.SelectedMetadata is not ResourcePath path)
            return;

        var dir = _directory.OpenSubdirectory(path);
        _replayMan.LoadReplay(dir);
    }

    private void RefreshReplays()
    {
        _mainMenuControl.ReplaySelect.Clear();

        int i = 0;

        if (_directory != null)
        {
            foreach (var file in _directory.DirectoryEntries(ResourcePath.Root))
            {
                var path = new ResourcePath(file).ToRootedPath();
                if (!_directory.Exists(path / Manager.ReplayManager.YamlFilename))
                    continue;

                _mainMenuControl.ReplaySelect.AddItem(file, i);
                _mainMenuControl.ReplaySelect.SetItemMetadata(_mainMenuControl.ReplaySelect.GetIdx(i), path);
                i++;
            }
        }

        if (i == 0)
        {
            _mainMenuControl.LoadButton.Disabled = true;
            _mainMenuControl.ReplaySelect.Disabled = true;
            _mainMenuControl.ReplaySelect.AddItem("<No Replays>");
        }
        else
        {
            _mainMenuControl.LoadButton.Disabled = false;
            _mainMenuControl.ReplaySelect.Disabled = false;
        }
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
