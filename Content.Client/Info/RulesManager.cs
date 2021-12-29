using System;
using System.Globalization;
using Content.Client.Lobby;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Info;

public sealed class RulesManager : SharedRulesManager
{
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>(OnShowRulesPopupMessage);
        _stateManager.OnStateChanged += OnStateChanged;
    }

    private void OnShowRulesPopupMessage(ShowRulesPopupMessage message)
    {
        ShowRules(message.PopupTime);
    }

    private void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is not (GameScreen or LobbyState))
            return;
        _stateManager.OnStateChanged -= OnStateChanged;

        var path = new ResourcePath($"/rules_last_seen_{_configManager.GetCVar(CCVars.ServerId)}");
        var showRules = true;
        if (_resource.UserData.TryReadAllText(path, out var lastReadTimeText)
            && DateTime.TryParse(lastReadTimeText, null, DateTimeStyles.AssumeUniversal, out var lastReadTime))
            showRules = lastReadTime < DateTime.UtcNow - TimeSpan.FromDays(60);
        else
            SaveLastReadTime();

        if (!showRules)
            return;

        ShowRules(_configManager.GetCVar(CCVars.RulesWaitTime));
    }

    /// <summary>
    ///     Ran when the user opens ("read") the rules, stores the new ID to disk.
    /// </summary>
    public void SaveLastReadTime()
    {
        using var sw = _resource.UserData.OpenWriteText(new ResourcePath($"/rules_last_seen_{_configManager.GetCVar(CCVars.ServerId)}"));

        sw.Write(DateTime.UtcNow.ToUniversalTime());
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
        SaveLastReadTime();
    }
}
