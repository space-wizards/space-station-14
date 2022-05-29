using Content.Client.Lobby;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client.Info;

public sealed class RulesManager : SharedRulesManager
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private bool _shouldShowRules;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ShouldShowRulesPopupMessage>(OnShouldShowRules);
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>(OnShowRulesPopupMessage);
        _netManager.RegisterNetMessage<RulesAcceptedMessage>();
        _stateManager.OnStateChanged += OnStateChanged;
    }

    private void OnShouldShowRules(ShouldShowRulesPopupMessage message)
    {
        _shouldShowRules = true;
    }

    private void OnShowRulesPopupMessage(ShowRulesPopupMessage message)
    {
        ShowRules(message.PopupTime);
    }

    private void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is not (GameScreen or LobbyState))
            return;

        if (!_shouldShowRules)
            return;

        _shouldShowRules = false;

        ShowRules(_configManager.GetCVar(CCVars.RulesWaitTime));
    }

    private void ShowRules(float time)
    {
        var rulesPopup = new RulesPopup
        {
            Timer = time
        };
        rulesPopup.OnQuitPressed += OnQuitPressed;
        rulesPopup.OnAcceptPressed += OnAcceptPressed;
        _userInterfaceManager.RootControl.AddChild(rulesPopup);
    }

    private void OnQuitPressed()
    {
        _consoleHost.ExecuteCommand("quit");
    }

    private void OnAcceptPressed()
    {
        var message = _netManager.CreateNetMessage<RulesAcceptedMessage>();
        _netManager.ClientSendMessage(message);
    }
}
