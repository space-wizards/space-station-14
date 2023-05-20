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
using Robust.Shared.ContentPack;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Replay.UI.Menu;

/// <summary>
///     Main menu screen for selecting and loading replays.
/// </summary>
public sealed class ReplayMainScreen : State
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IResourceManager _resourceMan = default!;
    [Dependency] private readonly Manager.ReplayManager _replayMan = default!;
    [Dependency] private readonly IReplayLoadManager _loadMan = default!;
    [Dependency] private readonly IGameController _controllerProxy = default!;
    [Dependency] private readonly IClientRobustSerializer _serializer = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    private ReplayMainMenuControl _mainMenuControl = default!;

    // TODO cvar (or add a open-file dialog?).
    public static readonly ResPath DefaultReplayDirectory = new("/replays");

    // Should probably be localized, but should never happen, so...
    public const string Error = "Error";

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
        _mainMenuControl.ReplaySelect.SelectId(obj.Id);
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
            || _loadMan.LoadYamlMetadata(_directory.OpenSubdirectory(path)) is not { } data)
        {
            info.SetMarkup(Loc.GetString("replay-info-none"));
            info.HorizontalAlignment = Control.HAlignment.Center;
            info.VerticalAlignment = Control.VAlignment.Center;
            _mainMenuControl.LoadButton.Disabled = true;
            return;
        }

        var file = path.ToRelativePath().ToString();
        data.TryGet<ValueDataNode>("time", out var timeNode);
        data.TryGet<ValueDataNode>("duration", out var durationNode);
        data.TryGet<ValueDataNode>("roundId", out var roundIdNode);
        data.TryGet<ValueDataNode>("engineVersion", out var engineNode);
        data.TryGet<ValueDataNode>("buildForkId", out var forkNode);
        data.TryGet<ValueDataNode>("buildVersion", out var versionNode);
        data.TryGet<ValueDataNode>("typeHash", out var hashNode);
        var forkVersion = versionNode?.Value ?? Error;
        DateTime.TryParse((string?) timeNode?.Value, out var time);
        TimeSpan.TryParse((string?) durationNode?.Value, out var duration);

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

        // TODO REPLAYS version information
        // Include engine, forkId, & forkVersion data with the client.
        // Currently these are server-exclusive CVARS.
        // If they differ from the current file, highlight the text in yellow (file may be loadable, but may cause issues).

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
            ("engVersion", engineNode?.Value ?? Error),
            ("hash", typeHash)));
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
                if (!_directory.Exists(path / IReplayRecordingManager.MetaFile.ToRelativePath()))
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
