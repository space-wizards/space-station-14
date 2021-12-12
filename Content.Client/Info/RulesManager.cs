using System;
using System.Globalization;
using System.IO;
using Content.Client.HUD;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Info;

public sealed class RulesManager
{
    [Dependency] private readonly IClientNetManager _clientNetManager = default!;
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    public event Action? OpenRulesAndInfoWindow;

    private void OnConnectStateChanged(ClientConnectionState state)
    {
        if (state != ClientConnectionState.Connected)
            return;

        var path = new ResourcePath($"/rules_last_seen_{_configManager.GetCVar(CCVars.ServerId)}");
        var showRules = true;
        if (_resource.UserData.Exists(path)
            && DateTime.TryParse(_resource.UserData.ReadAllText(path), null, DateTimeStyles.AssumeUniversal,
                out var lastReadTime))
            showRules = lastReadTime < DateTime.UtcNow - TimeSpan.FromDays(60);
        else
            SaveLastReadTime();

        if (showRules)
            OpenRulesAndInfoWindow?.Invoke();
    }

    /// <summary>
    ///     Ran when the user opens ("read") the rules, stores the new ID to disk.
    /// </summary>
    public void SaveLastReadTime()
    {
        using var file = _resource.UserData.Create(new ResourcePath($"/rules_last_seen_{_configManager.GetCVar(CCVars.ServerId)}"));
        using var sw = new StreamWriter(file);

        sw.Write(DateTime.UtcNow.ToUniversalTime());
    }

    public void Initialize()
    {
        _clientNetManager.ClientConnectStateChanged += OnConnectStateChanged;
    }
}
