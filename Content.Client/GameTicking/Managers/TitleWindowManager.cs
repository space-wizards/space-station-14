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
        _cfg.OnValueChanged(CVars.GameHostName, _ => OnHostnameChange(), true);
        _cfg.OnValueChanged(CCVars.GameHostnameInTitlebar, _ => OnHostnameChange(), true);

        _client.RunLevelChanged += (_, _) => OnHostnameChange();
    }

    private void OnHostnameChange()
    {
        var defaultWindowTitle = _gameController.GameTitle();

        // When the client starts connecting, it will be using either the default hostname, or whatever hostname
        // is set in its config file (aka the last server they connected to) until it receives the latest cvars.
        // If they are not connected then we will not show anything other than the usual window title.
        if (_client.RunLevel != ClientRunLevel.InGame)
        {
            _clyde.SetWindowTitle(defaultWindowTitle);
            return;
        }

        _clyde.SetWindowTitle(
            _cfg.GetCVar(CCVars.GameHostnameInTitlebar)
                ? _cfg.GetCVar(CVars.GameHostName) + " - " + defaultWindowTitle
                : defaultWindowTitle);
    }
    // You thought I would remove the :3 from this code? You were wrong.
}
