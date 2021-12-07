using System;
using System.Globalization;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Info;

public sealed class RulesManager
{
    [Dependency] private readonly IClientNetManager _clientNetManager = default!;

    // If you fork SS14, change this to have the rules "last seen" date stored separately.
    public const string ForkId = "Wizards";

    [Dependency] private readonly IResourceManager _resource = default!;

    public event Action? OpenRulesWindow;

    private void OnConnectStateChanged(ClientConnectionState state)
    {
        if (state != ClientConnectionState.Connected)
            return;

        var path = new ResourcePath($"/rules_last_seen_{ForkId}");
        var lastReadTime = DateTime.UnixEpoch;
        if (_resource.UserData.Exists(path))
        {
            lastReadTime = DateTime.Parse(_resource.UserData.ReadAllText(path), null, DateTimeStyles.AssumeUniversal);
        }

        var showRules = lastReadTime < DateTime.UtcNow - TimeSpan.FromDays(60);

        if (showRules)
            OpenRulesWindow?.Invoke();
    }

    /// <summary>
    ///     Ran when the user opens ("read") the rules, stores the new ID to disk.
    /// </summary>
    public void SaveLastReadTime()
    {
        using var file = _resource.UserData.Create(new ResourcePath($"/rules_last_seen_{ForkId}"));
        using var sw = new StreamWriter(file);

        sw.Write(DateTime.UtcNow.ToUniversalTime());
    }

    public void Initialize()
    {
        _clientNetManager.ClientConnectStateChanged += OnConnectStateChanged;
    }
}
