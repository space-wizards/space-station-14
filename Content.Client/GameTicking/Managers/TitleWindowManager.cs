using Content.Shared.CCVar;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Client.GameTicking.Managers;

public sealed class TitleWindowManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameController _gameController = default!;

    public void Initialize()
    {
        _cfg.OnValueChanged(CVars.GameHostName, OnHostnameChange, true);
        _cfg.OnValueChanged(CCVars.GameHostnameInTitlebar, OnHostnameTitleChange, true);

        _client.RunLevelChanged += OnRunLevelChangedChange;
    }

    public void Shutdown()
    {
        _cfg.UnsubValueChanged(CVars.GameHostName, OnHostnameChange);
        _cfg.UnsubValueChanged(CCVars.GameHostnameInTitlebar, OnHostnameTitleChange);
    }

    private void OnHostnameChange(string hostname)
    {
        var defaultWindowTitle = _gameController.GameTitle();

        // Since the game assumes the server name is MyServer and that GameHostnameInTitlebar CCVar is true by default
        // Lets just... not show anything. This also is used to revert back to just the game title on disconnect.
        if (_client.RunLevel == ClientRunLevel.Initialize)
        {
            _clyde.SetWindowTitle(defaultWindowTitle);
            return;
        }

        if (_cfg.GetCVar(CCVars.GameHostnameInTitlebar))
            // If you really dislike the dash I guess change it here
            _clyde.SetWindowTitle(hostname + " - " + defaultWindowTitle);
        else
            _clyde.SetWindowTitle(defaultWindowTitle);
    }

    // Clients by default assume game.hostname_in_titlebar is true
    // but we need to clear it as soon as we join and actually receive the servers preference on this.
    // This will ensure we rerun OnHostnameChange and set the correct title bar name.
    private void OnHostnameTitleChange(bool colonthree)
    {
        OnHostnameChange(_cfg.GetCVar(CVars.GameHostName));
    }

    // This is just used we can rerun the hostname change function when we disconnect to revert back to just the games title.
    private void OnRunLevelChangedChange(object? sender, RunLevelChangedEventArgs runLevelChangedEventArgs)
    {
        OnHostnameChange(_cfg.GetCVar(CVars.GameHostName));
    }
}
