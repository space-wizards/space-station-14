using Content.Client.Lobby;
using Content.Client.Gameplay;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
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
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    private InfoSection rulesSection = new InfoSection("", "", false);
    private bool _shouldShowRules = false;

    private RulesPopup? _activePopup;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ShouldShowRulesPopupMessage>(OnShouldShowRules);
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>(OnShowRulesPopupMessage);
        _netManager.RegisterNetMessage<RulesAcceptedMessage>();
        _stateManager.OnStateChanged += OnStateChanged;

        _consoleHost.RegisterCommand("fuckrules", "", "", (_, _, _) =>
        {
            OnAcceptPressed();
        });
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
        if (args.NewState is not (GameplayState or LobbyState))
            return;

        if (!_shouldShowRules)
            return;

        _shouldShowRules = false;

        ShowRules(_configManager.GetCVar(CCVars.RulesWaitTime));
    }

    private void ShowRules(float time)
    {
        if (_activePopup != null)
            return;

        _activePopup = new RulesPopup
        {
            Timer = time
        };

        _activePopup.OnQuitPressed += OnQuitPressed;
        _activePopup.OnAcceptPressed += OnAcceptPressed;
        _userInterfaceManager.WindowRoot.AddChild(_activePopup);
        LayoutContainer.SetAnchorPreset(_activePopup, LayoutContainer.LayoutPreset.Wide);
    }

    private void OnQuitPressed()
    {
        _consoleHost.ExecuteCommand("quit");
    }

    private void OnAcceptPressed()
    {
        _netManager.ClientSendMessage(new RulesAcceptedMessage());

        _activePopup?.Orphan();
        _activePopup = null;
    }

    public void UpdateRules()
    {
        var rules = _sysMan.GetEntitySystem<InfoSystem>().Rules;
        rulesSection.SetText(rules.Title, rules.Text, true);
    }

    public Control RulesSection()
    {
        rulesSection = new InfoSection("", "", false);
        UpdateRules();
        return rulesSection;
    }
}
