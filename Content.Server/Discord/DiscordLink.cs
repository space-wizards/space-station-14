using System.Diagnostics;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Discord;
using Discord.WebSocket;
using Robust.Shared.Configuration;
using LogMessage = Discord.LogMessage;

namespace Content.Server.Discord;

public sealed class DiscordLink : IPostInjectInit
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    public DiscordSocketClient? Client;
    private ISawmill _sawmill = default!;

    private string _guildId = string.Empty;

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill("discord.link");

        Client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All // I FUCKIGN hate this. I do not care enough for me to actually set proper intents. TODO: Change this I guess?
        });
        Client.Log += Log;

        try
        {
            _configuration.OnValueChanged(CCVars.DiscordGuildId, OnGuildIdChanged, true);
        }
        catch (Exception e)
        {
            _sawmill.Fatal($"Failed to setup cvar.");
            return;
        }

        if (_configuration.GetCVar(CCVars.DiscordToken) is not { } token)
        {
            _sawmill.Warning("No Discord token specified, not connecting.");
            return;
        }

        #region Events

        Client.Ready += () =>
        {
            _sawmill.Info("Discord client ready.");
            return Task.CompletedTask;
        };

        #endregion

        Task.Run(() =>
        {
            try
            {
                LoginAsync(token);
            }
            catch (AggregateException e)
            {
                _sawmill.Error("Failed to connect to Discord!", e);
            }
        });
    }

    private void OnGuildIdChanged(string guildId)
    {
        _guildId = guildId;
    }

    private async Task LoginAsync(string token)
    {
        Debug.Assert(Client != null);
        Debug.Assert(Client.LoginState == LoginState.LoggedOut);

        await Client.LoginAsync(TokenType.Bot, token);
        await Client.StartAsync();

        _sawmill.Info("Connected to Discord.");
        await Task.Delay(-1); // Following discord.net guide, idk what im doiung help
    }

    private string FormatLog(LogMessage msg)
    {
        return msg.Exception is null ? $"{msg.Source}: {msg.Message}" : $"{msg.Source}: {msg.Exception}\n{msg.Message}";
    }

    private Task Log(LogMessage msg)
    {
        switch (msg.Severity)
        {
            case LogSeverity.Critical:
                _sawmill.Fatal(FormatLog(msg));
                break;
            case LogSeverity.Error:
                _sawmill.Error(FormatLog(msg));
                break;
            case LogSeverity.Warning:
                _sawmill.Warning(FormatLog(msg));
                break;
            default: // Info Verbose and Debug
                _sawmill.Debug(FormatLog(msg));
                break;
        }

        return Task.CompletedTask;
    }

    public void SendMessage(string message, ulong channel)
    {
        if (Client is null)
        {
            _sawmill.Error("Tried to send a Discord message but the client is null! Is the token not set?");
            return;
        }

        if (_guildId == string.Empty)
        {
            _sawmill.Error("Tried to send a Discord message but the guild ID is not set! Blow up now!");
            return;
        }

        Client.GetGuild(ulong.Parse(_guildId))
            .GetTextChannel(channel)
            .SendMessageAsync(message, false, null, null,AllowedMentions.None);
    }

    public SocketGuild GetGuild()
    {
        if (Client is null)
        {
            _sawmill.Error("Tried to get a Discord guild but the client is null! Is the token not set?");
            return null!;
        }

        if (_guildId == string.Empty)
        {
            _sawmill.Error("Tried to get a Discord guild but the guild ID is not set! Blow up now!");
            return null!;
        }

        return Client.GetGuild(ulong.Parse(_guildId));
    }
}
