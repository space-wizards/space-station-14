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
/*
 * Rules unread should be per server to show that the rules are different.
 * Save server id
 * Save the last read date
 *
 * Last read date is used for comparing against rules change date sent from server
 * Last read date is used for reopening the rules after some time as a reminder (2 months?)
 *
 * Add delay before allowing the player to close.
 * Require scrolling to the bottom to close.
 * Make the close button an `I Agree` button.
 */

    // If you fork SS14, change this to have the rules "last seen" date stored separately.
    public const string ForkId = "Wizards";

    [Dependency] private readonly IResourceManager _resource = default!;

    public event Action? OpenRulesWindow;

    public void Startup()
    {
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

    private void OnConnectStateChanged(ClientConnectionState state)
    {
        if (state == ClientConnectionState.Connected)
            Startup();
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
