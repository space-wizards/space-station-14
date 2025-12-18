using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using NetCord;
using NetCord.Gateway;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;

namespace Content.Server.Discord.DiscordLink;

public sealed class DiscordChatLink : IPostInjectInit
{
    [Dependency] private readonly DiscordLink _discordLink = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private ulong? _oocChannelId;
    private ulong? _adminChannelId;

    public void Initialize()
    {
        _discordLink.OnMessageReceived += OnMessageReceived;

        #if DEBUG
        _discordLink.RegisterCommandCallback(OnDebugCommandRun, "debug");
        #endif

        _configurationManager.OnValueChanged(CCVars.OocDiscordChannelId, OnOocChannelIdChanged, true);
        _configurationManager.OnValueChanged(CCVars.AdminChatDiscordChannelId, OnAdminChannelIdChanged, true);
    }

    public void Shutdown()
    {
        _discordLink.OnMessageReceived -= OnMessageReceived;

        _configurationManager.UnsubValueChanged(CCVars.OocDiscordChannelId, OnOocChannelIdChanged);
        _configurationManager.UnsubValueChanged(CCVars.AdminChatDiscordChannelId, OnAdminChannelIdChanged);
    }

    #if DEBUG
    private void OnDebugCommandRun(CommandReceivedEventArgs ev)
    {
        var args = string.Join('\n', ev.Arguments);
        _sawmill.Info($"Provided arguments: \n{args}");
    }
    #endif

    private void OnOocChannelIdChanged(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            _oocChannelId = null;
            return;
        }

        _oocChannelId = ulong.Parse(channelId);
    }

    private void OnAdminChannelIdChanged(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            _adminChannelId = null;
            return;
        }

        _adminChannelId = ulong.Parse(channelId);
    }

    private void OnMessageReceived(Message message)
    {
        if (message.Author.IsBot)
            return;

        var contents = message.Content.ReplaceLineEndings(" ");

        if (message.ChannelId == _oocChannelId)
        {
            _taskManager.RunOnMainThread(() => _chatManager.SendHookOOC(message.Author.Username, contents));
        }
        else if (message.ChannelId == _adminChannelId)
        {
            _taskManager.RunOnMainThread(() => _chatManager.SendHookAdmin(message.Author.Username, contents));
        }
    }

    public async void SendMessage(string message, string author, ChatChannel channel)
    {
        var channelId = channel switch
        {
            ChatChannel.OOC => _oocChannelId,
            ChatChannel.AdminChat => _adminChannelId,
            _ => throw new InvalidOperationException("Channel not linked to Discord."),
        };

        if (channelId == null)
        {
            // Configuration not set up. Ignore.
            return;
        }

        // @ and < are both problematic for discord due to pinging. / is sanitized solely to kneecap links to murder embeds via blunt force
        message = message.Replace("@", "\\@").Replace("<", "\\<").Replace("/", "\\/");

        try
        {
            await _discordLink.SendMessageAsync(channelId.Value, $"**{channel.GetString()}**: `{author}`: {message}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending Discord message: {e}");
        }
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("discord.chat");
    }
}
