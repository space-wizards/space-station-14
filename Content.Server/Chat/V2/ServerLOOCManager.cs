using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Chat.V2;

public interface IServerLoocManager
{
    public const int LoocVoiceRange = 10;
    public bool LoocEnabled { get; }
    public bool DeadLoocEnabled { get; }
    public bool CritLoocEnabled{ get; }
}

public sealed class ServerLoocManager : IServerLoocManager
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    public bool LoocEnabled { get; private set; } = true;
    public bool DeadLoocEnabled { get; private set; }
    public bool CritLoocEnabled { get; private set; }

    public void Initialize()
    {
        _configuration.OnValueChanged(CCVars.LoocEnabled, OnLoocEnabledChanged, true);
        _configuration.OnValueChanged(CCVars.DeadLoocEnabled, OnDeadLoocEnabledChanged, true);
        _configuration.OnValueChanged(CCVars.CritLoocEnabled, OnCritLoocEnabledChanged, true);
    }

    private void OnLoocEnabledChanged(bool val)
    {
        if (LoocEnabled == val)
            return;

        LoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-looc-chat-enabled-message" : "chat-manager-looc-chat-disabled-message"));
    }

    private void OnDeadLoocEnabledChanged(bool val)
    {
        if (DeadLoocEnabled == val) return;

        DeadLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-dead-looc-chat-enabled-message" : "chat-manager-dead-looc-chat-disabled-message"));
    }

    private void OnCritLoocEnabledChanged(bool val)
    {
        if (CritLoocEnabled == val)
            return;

        CritLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-crit-looc-chat-enabled-message" : "chat-manager-crit-looc-chat-disabled-message"));
    }
}
