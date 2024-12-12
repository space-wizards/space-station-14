using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Discord;
using Discord.WebSocket;
using Robust.Shared.Configuration;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using LogMessage = Discord.LogMessage;

namespace Content.Server.Discord.DiscordLink;

/// <summary>
/// Represents the arguments for the <see cref="DiscordLink.OnCommandReceived"/> event.
/// </summary>
public record CommandReceivedEventArgs
{
    /// <summary>
    /// The command that was received. This is the first word in the message, after the bot prefix.
    /// </summary>
    public string Command { get; init; } = string.Empty;
    /// <summary>
    /// The arguments to the command. This is everything after the command, split by spaces.
    /// </summary>
    public string[] Arguments { get; init; } = Array.Empty<string>();
    /// <summary>
    /// Information about the message that the command was received from. This includes the message content, author, etc.
    /// Use this to reply to the message, delete it, etc.
    /// </summary>
    public SocketMessage Message { get; init; } = default!;
}

public sealed class DiscordLink : IPostInjectInit
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;

    /// <summary>
    ///    The Discord client. This is null if the bot is not connected.
    /// </summary>
    /// <remarks>
    ///     This should not be used directly outside of DiscordLink. So please do not make it public. Use the methods in this class instead.
    /// </remarks>
    private DiscordSocketClient? _client;
    private ISawmill _sawmill = default!;
    private ISawmill _sawmillNet = default!;

    private ulong _guildId;
    private string _botToken = string.Empty;

    private bool _registeredLinks = false;

    public string BotPrefix = default!;
    /// <summary>
    /// If the bot is currently connected to Discord.
    /// </summary>
    public bool IsConnected => _client != null;

    #region Events

    /// <summary>
    ///     Event that is raised when a command is received from Discord.
    /// </summary>
    public event Action<CommandReceivedEventArgs>? OnCommandReceived;
    /// <summary>
    ///     Event that is raised when a message is received from Discord. This is raised for every message, including commands.
    /// </summary>
    public event Action<SocketMessage>? OnMessageReceived;

    public void RegisterCommandCallback(Action<CommandReceivedEventArgs> callback, string command)
    {
        OnCommandReceived += args =>
        {
            if (args.Command == command)
                callback(args);
        };
    }

    #endregion

    public void Initialize()
    {
        _configuration.OnValueChanged(CCVars.DiscordGuildId, OnGuildIdChanged, true);
        _configuration.OnValueChanged(CCVars.DiscordPrefix, OnPrefixChanged, true);
        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All
        });
        _client.Log += Log;
        _client.MessageReceived += OnCommandReceivedInternal;
        _client.MessageReceived += OnMessageReceivedInternal;

        if (_configuration.GetCVar(CCVars.DiscordToken) is not { } token || token == string.Empty)
        {
            _sawmill.Info("No Discord token specified, not connecting.");
            // The Bot is not connected, so we need to set the client to null, because some methods check if the bot is connected using a null check on the client.
            _client = null;
            return;
        }

        // If the Guild ID is empty OR the prefix is empty, we don't want to connect to Discord.
        if (_configuration.GetCVar(CCVars.DiscordGuildId) == string.Empty || _configuration.GetCVar(CCVars.DiscordPrefix) == string.Empty)
        {
            // This is a warning, not info, because it's a configuration error.
            // It is valid to not have a Discord token set which is why the above check is an info.
            // But if you have a token set, you should also have a guild ID and prefix set.
            _sawmill.Warning("No Discord guild ID or prefix specified, not connecting.");
            _client = null;
            return;
        }

        _botToken = token;
        // Since you cannot change the token while the server is running / the DiscordLink is initialized,
        // we can just set the token without updating it every time the cvar changes.

        _client.Ready += () =>
        {
            _sawmill.Info("Discord client ready.");
            return Task.CompletedTask;
        };

        Task.Run(async () =>
        {
            try
            {
                await LoginAsync(token);
            }
            catch (Exception e)
            {
                _sawmill.Error("Failed to connect to Discord!", e);
            }
        });
    }

    public void Shutdown()
    {
        if (_client != null)
        {
            _sawmill.Info("Disconnecting from Discord.");
            _client.Log -= Log;

            // Unsubscribe from the events.
            _client.MessageReceived -= OnCommandReceivedInternal;
            _client.MessageReceived -= OnMessageReceivedInternal;

            _client.LogoutAsync();
            _client.StopAsync();
            _client.Dispose();
            _client = null;
        }

        _configuration.UnsubValueChanged(CCVars.DiscordGuildId, OnGuildIdChanged);
        _configuration.UnsubValueChanged(CCVars.DiscordPrefix, OnPrefixChanged);
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill("discord.link");
        _sawmillNet = _logManager.GetSawmill("discord.link.log");
    }

    private void OnGuildIdChanged(string guildId)
    {
        _guildId = ulong.TryParse(guildId, out var id) ? id : 0;
    }

    private void OnPrefixChanged(string prefix)
    {
        BotPrefix = prefix;
    }

    private async Task LoginAsync(string token)
    {
        DebugTools.Assert(_client != null);
        DebugTools.Assert(_client.LoginState == LoginState.LoggedOut);

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();


        _sawmill.Info("Connected to Discord.");
    }

    private string FormatLog(LogMessage msg)
    {
        return msg.Exception is null ? $"{msg.Source}: {msg.Message}" : $"{msg.Source}: {msg.Exception}\n{msg.Message}";
    }

    private Task Log(LogMessage msg)
    {
        var logLevel = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Fatal,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Debug
        };

        _sawmillNet.Log(logLevel, FormatLog(msg));
        return Task.CompletedTask;
    }

    private Task OnCommandReceivedInternal(SocketMessage message)
    {
        var content = message.Content;
        // If the message is too short to be a command, or doesn't start with the bot prefix, ignore it.
        if (content.Length <= BotPrefix.Length || !content.StartsWith(BotPrefix))
            return Task.CompletedTask;

        // Split the message into the command and the arguments.
        var split = content[BotPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 0)
            return Task.CompletedTask;  // No command.

        // Raise the event!
        OnCommandReceived?.Invoke(new CommandReceivedEventArgs
        {
            Command = split[0],
            Arguments = split.Skip(1).ToArray(),
            Message = message
        });
        return Task.CompletedTask;
    }

    private Task OnMessageReceivedInternal(SocketMessage message)
    {
        OnMessageReceived?.Invoke(message);
        return Task.CompletedTask;
    }

    #region Proxy methods

    /// <summary>
    /// Sends a message to a Discord channel with the specified ID. Without any mentions.
    /// </summary>
    public async Task SendMessageAsync(ulong channelId, string message)
    {
        if (_client == null)
        {
            return;
        }

        var channel = _client.GetChannel(channelId) as IMessageChannel;
        if (channel == null)
        {
            _sawmill.Error("Tried to send a message to Discord but the channel was not found.");
            return;
        }

        await channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
    }

    #endregion
}
