using System;
using System.Globalization;
using Content.Client.Lobby;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.Info;

public sealed class RulesManager
{
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private RulesPopup? _rulesPopup;

    public void Initialize()
    {
        _stateManager.OnStateChanged += OnStateChanged;
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

        _rulesPopup = new RulesPopup();
        _userInterfaceManager.RootControl.AddChild(_rulesPopup);
    }

    /// <summary>
    ///     Ran when the user opens ("read") the rules, stores the new ID to disk.
    /// </summary>
    public void SaveLastReadTime()
    {
        using var sw = _resource.UserData.OpenWriteText(new ResourcePath($"/rules_last_seen_{_configManager.GetCVar(CCVars.ServerId)}"));

        sw.Write(DateTime.UtcNow.ToUniversalTime());
    }
}
