using System.Globalization;
using Content.Client.Gameplay;
using Content.Client.Guidebook;
using Content.Client.Info;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Info;

public sealed class InfoUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private RulesPopup? _rulesPopup;
    private RulesAndInfoWindow? _infoWindow;

    private static DateTime NextRulesReadTime => DateTime.UtcNow + TimeSpan.FromDays(60);

    public override void Initialize()
    {
        base.Initialize();

        _client.PlayerJoinedServer += OnJoinedServer;
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>(OnShowRulesPopupMessage);

        _consoleHost.RegisterCommand("fuckrules",
            "",
            "",
            (_, _, _) =>
        {
            OnAcceptPressed();
        });
    }

    private void OnJoinedServer(object? sender, PlayerEventArgs args)
    {
        if (_playerManager.LocalSession is not { } localSession)
            return;

        if (_adminManager.IsAdmin(localSession) && _cfg.GetCVar(CCVars.RulesExemptLocal))
            return;

        var nextReadTarget = DateTime.Parse(_cfg.GetCVar(CCVars.RulesNextPopupTime));
        if (nextReadTarget >= DateTime.UtcNow)
            return;

        var time = _cfg.GetCVar(CCVars.RulesWaitTime);
        ShowRules(time);
    }

    private void OnShowRulesPopupMessage(ShowRulesPopupMessage message)
    {
        ShowRules(message.PopupTime);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_infoWindow == null)
            return;

        _infoWindow.Dispose();
        _infoWindow = null;
    }

    private void ShowRules(float time)
    {
        if (_rulesPopup != null)
            return;

        _rulesPopup = new RulesPopup
        {
            Timer = time
        };

        _rulesPopup.OnQuitPressed += OnQuitPressed;
        _rulesPopup.OnAcceptPressed += OnAcceptPressed;
        UIManager.WindowRoot.AddChild(_rulesPopup);
        LayoutContainer.SetAnchorPreset(_rulesPopup, LayoutContainer.LayoutPreset.Wide);
    }

    private void OnQuitPressed()
    {
        _consoleHost.ExecuteCommand("quit");
    }

    private void OnAcceptPressed()
    {
        _cfg.SetCVar(CCVars.RulesNextPopupTime, NextRulesReadTime.ToString(CultureInfo.InvariantCulture));
        _cfg.SaveToFile();

        _rulesPopup?.Orphan();
        _rulesPopup = null;
    }

    public GuideEntryPrototype GetCoreRuleEntry()
    {
        var guide = _cfg.GetCVar(CCVars.RulesFile);
        var guideEntryPrototype = _prototype.Index<GuideEntryPrototype>(guide);
        return guideEntryPrototype;
    }

    public void OpenWindow()
    {
        if (_infoWindow == null || _infoWindow.Disposed)
            _infoWindow = UIManager.CreateWindow<RulesAndInfoWindow>();

        _infoWindow?.OpenCentered();
    }
}
