using System.IO.Compression;
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
using static Robust.Shared.Replays.ReplayConstants;

namespace Content.Replay.Menu;

/// <summary>
/// Main menu screen for selecting and loading replays.
/// </summary>
public sealed class ReplayMainScreen : State
{
    [Dependency] private readonly IResourceManager _resMan = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IReplayLoadManager _loadMan = default!;
    [Dependency] private readonly IClientResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameController _controllerProxy = default!;
    [Dependency] private readonly IClientRobustSerializer _serializer = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    private ReplayMainMenuControl _mainMenuControl = default!;
    private SelectReplayWindow? _selectWindow;
    private ResPath _directory;
    private List<(string Name, ResPath Path)> _replays = new();
    private ResPath? _selected;

    protected override void Startup()
    {
        _mainMenuControl = new(_resourceCache);
        _userInterfaceManager.StateRoot.AddChild(_mainMenuControl);

        _mainMenuControl.SelectButton.OnPressed += OnSelectPressed;
        _mainMenuControl.QuitButton.OnPressed += QuitButtonPressed;
        _mainMenuControl.OptionsButton.OnPressed += OptionsButtonPressed;
        _mainMenuControl.FolderButton.OnPressed += OnFolderPressed;
        _mainMenuControl.LoadButton.OnPressed += OnLoadpressed;

        _directory = new ResPath(_cfg.GetCVar(CVars.ReplayDirectory)).ToRootedPath();
        RefreshReplays();
        SelectReplay(_replays.FirstOrNull()?.Path);
        if (_selected == null) // force initial update
            UpdateSelectedInfo();
    }

    /// <summary>
    /// Read replay meta-data and update the replay info box.
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

        using var fileReader = new ReplayFileReaderZip(
            new ZipArchive(_resMan.UserData.OpenRead(replay)), ReplayZipFolder);
        if (!_resMan.UserData.Exists(replay)
            || _loadMan.LoadYamlMetadata(fileReader) is not { } data)
        {
            info.SetMarkup(Loc.GetString("replay-info-invalid"));
            info.HorizontalAlignment = Control.HAlignment.Center;
            info.VerticalAlignment = Control.VAlignment.Center;
            _mainMenuControl.LoadButton.Disabled = true;
            return;
        }

        var file = replay.ToRelativePath().ToString();
        data.TryGet<ValueDataNode>(MetaKeyTime, out var timeNode);
        data.TryGet<ValueDataNode>(MetaFinalKeyDuration, out var durationNode);
        data.TryGet<ValueDataNode>("roundId", out var roundIdNode);
        data.TryGet<ValueDataNode>(MetaKeyTypeHash, out var hashNode);
        data.TryGet<ValueDataNode>(MetaKeyComponentHash, out var compHashNode);
        DateTime.TryParse(timeNode?.Value, out var time);
        TimeSpan.TryParse(durationNode?.Value, out var duration);

        var forkId = string.Empty;
        if (data.TryGet<ValueDataNode>(MetaKeyForkId, out var forkNode))
        {
            // TODO Replay client build info.
            // When distributing the client we need to distribute a build.json or provide these cvars some other way?
            var clientFork = _cfg.GetCVar(CVars.BuildForkId);
            if (string.IsNullOrWhiteSpace(clientFork))
                forkId = forkNode.Value;
            else if (forkNode.Value == clientFork)
                forkId = $"[color=green]{forkNode.Value}[/color]";
            else
                forkId = $"[color=yellow]{forkNode.Value}[/color]";
        }

        var forkVersion = string.Empty;
        if (data.TryGet<ValueDataNode>(MetaKeyForkVersion, out var versionNode))
        {
            forkVersion = versionNode.Value;
            // Why does this not have a try-convert function? I just want to check if it looks like a hash code.
            try
            {
                Convert.FromHexString(forkVersion);
                // version is a probably some git commit. Crop it to keep the info box small.
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

        if (hashNode == null)
            throw new Exception("Invalid metadata file. Missing type hash");

        var typeHash = hashNode.Value;
        _mainMenuControl.LoadButton.Disabled = false;
        if (Convert.FromHexString(typeHash).SequenceEqual(_serializer.GetSerializableTypesHash()))
        {
            typeHash = $"[color=green]{typeHash[..16]}[/color]";
        }
        else
        {
            typeHash = $"[color=red]{typeHash[..16]}[/color]";
            _mainMenuControl.LoadButton.Disabled = true;
        }

        if (compHashNode == null)
            throw new Exception("Invalid metadata file. Missing component hash");

        var compHash = compHashNode.Value;
        if (Convert.FromHexString(compHash).SequenceEqual(_factory.GetHash(true)))
        {
            compHash = $"[color=green]{compHash[..16]}[/color]";
            _mainMenuControl.LoadButton.Disabled = false;
        }
        else
        {
            compHash = $"[color=red]{compHash[..16]}[/color]";
            _mainMenuControl.LoadButton.Disabled = true;
        }

        var engineVersion = string.Empty;
        if (data.TryGet<ValueDataNode>(MetaKeyEngineVersion, out var engineNode))
        {
            var clientVer = _cfg.GetCVar(CVars.BuildEngineVersion);
            if (string.IsNullOrWhiteSpace(clientVer))
                engineVersion = engineNode.Value;
            else if (engineNode.Value == clientVer)
                engineVersion = $"[color=green]{engineNode.Value}[/color]";
            else
                engineVersion = $"[color=yellow]{engineNode.Value}[/color]";
        }

        // Strip milliseconds. Apparently there is no general format string that suppresses milliseconds.
        duration = new((int)Math.Floor(duration.TotalDays), duration.Hours, duration.Minutes, duration.Seconds);

        data.TryGet<ValueDataNode>(MetaKeyName, out var nameNode);
        var name = nameNode?.Value ?? string.Empty;

        info.HorizontalAlignment = Control.HAlignment.Left;
        info.VerticalAlignment = Control.VAlignment.Top;

        info.SetMarkup(Loc.GetString(
            "replay-info-info",
            ("file", file),
            ("name", name),
            ("time", time),
            ("roundId", roundIdNode?.Value ?? "???"),
            ("duration", duration),
            ("forkId", forkId),
            ("version", forkVersion),
            ("engVersion", engineVersion),
            ("compHash", compHash),
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
        {
            var fileReader = new ReplayFileReaderZip(
                new ZipArchive(_resMan.UserData.OpenRead(_selected.Value)), ReplayZipFolder);
            _loadMan.LoadAndStartReplay(fileReader);
        }
    }

    private void RefreshReplays()
    {
        _replays.Clear();

        foreach (var entry in _resMan.UserData.DirectoryEntries(_directory))
        {
            var file = _directory / entry;
            try
            {
                using var fileReader = new ReplayFileReaderZip(
                    new ZipArchive(_resMan.UserData.OpenRead(file)), ReplayZipFolder);

                var data = _loadMan.LoadYamlMetadata(fileReader);
                if (data == null)
                    continue;

                var name = data.Get<ValueDataNode>(MetaKeyName).Value;
                _replays.Add((name, file));

            }
            catch
            {
                // Ignore file
            }
        }

        _selectWindow?.Repopulate(_replays);
        if (_selected.HasValue && _replays.All(x => x.Path != _selected.Value))
            SelectReplay(null);
        else
            _selectWindow?.UpdateSelected(_selected);
    }

    public void SelectReplay(ResPath? replay)
    {
        if (_selected == replay)
            return;

        _selected = replay;
        try
        {
            UpdateSelectedInfo();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load replay info. Exception: {ex}");
            SelectReplay(null);
            return;
        }
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
