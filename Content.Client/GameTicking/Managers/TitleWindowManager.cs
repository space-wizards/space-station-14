using Content.Shared.CCVar;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Client.GameTicking.Managers;

public sealed class TitleWindowManager
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameController _gameController = default!;

    public void Initialize()
    {
        _cfg.OnValueChanged(CVars.GameHostName, OnHostnameChange, true);
        _cfg.OnValueChanged(CCVars.HostnameInTitlebar, OnHostnameTitleChange, true);
    }

    public void Shutdown()
    {
        _cfg.UnsubValueChanged(CVars.GameHostName, OnHostnameChange);
        _cfg.UnsubValueChanged(CVars.HostnameInTitlebar, OnHostnameTitleChange);
    }

    // This may use the last joined server temporarily until the CCVars of the joining server are received.
    private void OnHostnameChange(string hostname)
    {
        var defaultWindowTitle = _gameController.GameTitle();

        if (_cfg.GetCVar(CCVars.HostnameInTitlebar))
            // If you really dislike the dash I guess change it here
            _clyde.SetWindowTitle(hostname + " - " + defaultWindowTitle);
        else
            _clyde.SetWindowTitle(defaultWindowTitle);
    }

    // Clients by default assume game.hostname_in_titlebar is true (and also that the server hostname is MyServer)
    // but we need to clear it as soon as we join and actually receive the CCVar.
    // This will ensure we rerun OnHostnameChange and set the correct title bar name.
    // We could also initialize this later but uhh I was told to make this a manager and put it into entrypoint.
    private void OnHostnameTitleChange(bool colonthree)
    {
        OnHostnameChange(_cfg.GetCVar(CVars.GameHostName));
    }
}
