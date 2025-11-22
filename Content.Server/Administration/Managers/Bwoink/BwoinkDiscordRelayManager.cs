using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Afk;
using Content.Server.Discord.DiscordLink;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Content.Shared.CCVar;
using NetCord.Rest;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using MessageFlags = Content.Shared.Administration.Managers.Bwoink.MessageFlags;

namespace Content.Server.Administration.Managers.Bwoink;

/// <summary>
/// Handles all the logic around the <see cref="DiscordRelay"/> channel feature.
/// </summary>
public sealed class BwoinkDiscordRelayManager : IPostInjectInit
{
    [Dependency] private readonly ILogManager _logManager = null!;
    [Dependency] private readonly DiscordLink _discordLink = null!;
    [Dependency] private readonly ServerBwoinkManager _serverBwoinkManager = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;
    [Dependency] private readonly ITaskManager _taskManager = null!;
    [Dependency] private readonly IConfigurationManager _configurationManager = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = null!;
    [Dependency] private readonly ILocalizationManager _localizationManager = null!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;

    // ReSharper disable once InconsistentNaming
    private ISawmill Log = null!;

    /// <summary>
    /// Our dictionary of messages we have not sent yet.
    /// </summary>
    private readonly Dictionary<ProtoId<BwoinkChannelPrototype>, Queue<NetUserId>>
        _channelQueue = new();

    /// <summary>
    /// Holds the resolved bwoink channel to the Discord relay channel (if there is one). Updated whenever <see cref="ReloadedData"/> is called.
    /// </summary>
    private readonly Dictionary<ProtoId<BwoinkChannelPrototype>, ulong?> _channelToChannelMapping = new();

    /// <summary>
    /// When we should next process the queue.
    /// </summary>
    private TimeSpan _nextUpdate = TimeSpan.MinValue;

    /// <summary>
    /// How often to process the queue.
    /// </summary>
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The task for consuming the queue.
    /// Only ever null if we have never updated before. When setting this value check if the previous value was completed.
    /// </summary>
    private Task? _update;
    private readonly Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, List<DiscordRelayMessage>>> _activeMessages = new();

    private const int MaxCharsPerEmbed = 4000;
    private const int MaxCharsPerMessage = 2000;

    void IPostInjectInit.PostInject()
    {
        Log = _logManager.GetSawmill("bwoink.discord");
    }

    public void Initialize()
    {
        _serverBwoinkManager.MessageReceived += BwoinkReceived;
        _serverBwoinkManager.ReloadedData += ReloadedData;

        ReloadedData();
    }

    public void Shutdown()
    {
        if (_channelQueue.Values.Sum(x => x.Count) > 0 && _update != null)
            _update = UpdateAsync(); // queue in a last update so shutdown doesn't eat any messages.

        if (_update != null)
            _taskManager.BlockWaitOnTask(_update);
    }

    public void Update()
    {
        if (_gameTiming.RealTime < _nextUpdate)
            return;

        _nextUpdate = _gameTiming.RealTime + UpdateInterval;

        if (_update is { IsCompleted: false })
        {
            Log.Warning("Tried to update relay while relay update still in progress! Slow connection to Discord?");
            return;
        }

        if (_update is { IsFaulted: true })
        {
            Log.Error($"Failed to update queue: {_update.Exception}");
        }

        _update = UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        foreach (var (channel, queue) in _channelQueue)
        {
            if (!_channelToChannelMapping.TryGetValue(channel, out var channelId))
            {
                // Channel doesn't have a relay, so we just continue to next channel.
                queue.Clear();
                continue;
            }

            while (queue.TryDequeue(out var userChannel))
            {
                // Do we have an active message for this person already?
                if (!_activeMessages[channel].TryGetValue(userChannel, out var messages))
                {
                    _activeMessages[channel].Add(userChannel, [new DiscordRelayMessage()]);
                    messages = _activeMessages[channel][userChannel];
                }

                var allEmbeds = GetEmbedsForMessageHistory(channel, userChannel, messages).ToList();
                for (var i = 0; i < allEmbeds.Count; i++)
                {
                    var embed = allEmbeds[i];
                    var message = messages[i];
                    if (message.Done)
                        continue;

                    // Attempt to get the previous and next message (if any)
                    ulong? prevMessageId = null;
                    ulong? nextMessageId = null;
                    if (i != 0 && i - 1 < allEmbeds.Count)
                    {
                        prevMessageId = messages[i - 1].MessageId;
                    }

                    if (i != allEmbeds.Count - 1 && i + 1 < allEmbeds.Count)
                    {
                        nextMessageId = messages[i + 1].MessageId;
                    }

                    var sb = new StringBuilder();
                    if (prevMessageId.HasValue)
                        sb.AppendLine(_localizationManager.GetString("bwoink-discord-relay-prev", ("link", GenerateDiscordLink(channelId!.Value, prevMessageId.Value))));

                    if (nextMessageId.HasValue)
                        sb.AppendLine(_localizationManager.GetString("bwoink-discord-relay-next", ("link", GenerateDiscordLink(channelId!.Value, nextMessageId.Value))));

                    // Unsure if sending an empty message will cause netcord to null it, but better be safe.
                    var messageContent = sb.ToString();
                    if (messageContent.Length == 0)
                        messageContent = null;

                    if (prevMessageId.HasValue && nextMessageId.HasValue)
                    {
                        // We have now set *both*. We will now not have to touch this RestMessage ever again.
                        message.Done = true;
                    }

                    if (message.Message == null)
                    {
                        // No message, so we send a new one.
                        message.Message = await _discordLink.SendMessageAsync(channelId!.Value, messageContent, [embed]);
                        message.MessageId = message.Message?.Id;
                        if (allEmbeds.Count > 1)
                        {
                            // In order for the previous message we sent to receive the "done" flag, we need to queue an update again for this userchannel.
                            // This is because the check of this message happened *before* this message. And obviously we cannot predict the message id.
                            _channelQueue[channel].Enqueue(userChannel);
                        }
                    }
                    else
                    {
                        // We DO have a message. We try to update it.
                        message.Message = await message.Message.ModifyAsync(options =>
                        {
                            options.Embeds = [embed];
                            options.Content = messageContent;
                        });
                    }
                }

                // We have sent everything we need, we can now safely nuke any rest messages that are "done".
                foreach (var message in messages)
                {
                    if (!message.Done || message.Message == null)
                        continue;

                    message.Message = null;
                }
            }
        }
    }

    /// <summary>
    /// Helper method to generate a Discord message link.
    /// </summary>
    private string GenerateDiscordLink(ulong channel, ulong messageId)
    {
        return $"https://discord.com/channels/{_configurationManager.GetCVar(CCVars.DiscordGuildId)}/{channel}/{messageId}";
    }

    /// <summary>
    /// Generates the embeds for a conversation.
    /// </summary>
    private IEnumerable<EmbedProperties> GetEmbedsForMessageHistory(ProtoId<BwoinkChannelPrototype> channel, NetUserId userChannel, List<DiscordRelayMessage> relayMessages)
    {
        var messages = _serverBwoinkManager.GetConversationsForChannel(channel)[userChannel];

        var descBuilder = new StringBuilder();
        var messageBuilder = new StringBuilder();
        var formattedMessageSb = new StringBuilder();

        var embed = CreateEmbedFor(userChannel);
        var startIndex = 0;

        for (var i = 0; i < messages.Messages.Count; i++)
        {
            var message = messages.Messages[i];
            var formattedMessageObject = _serverBwoinkManager.FormatMessage(channel, message, useRoundTime: true);

            // may god forgive me, i have consumed glue when coding this
            foreach (var markupNode in formattedMessageObject)
            {
                if (markupNode.Name == null)
                {
                    if (markupNode.Value.StringValue == null)
                        continue; // ??

                    // just a simple text node
                    formattedMessageSb.Append(NetCord.Format.Escape(markupNode.Value.StringValue));
                    continue;
                }

                // not text, maybe a bold tag?
                if (markupNode.Name != "bold")
                    continue; // :( no it isn't

                formattedMessageSb.Append("**"); // lmao this shit will break the moment there is more than one bold tag
            }

            var formattedMessage = formattedMessageSb.ToString();
            formattedMessageSb.Clear();

            if (formattedMessage.Length > MaxCharsPerMessage)
            {
                const int truncatedLength = MaxCharsPerMessage - 1;

                formattedMessage = formattedMessage[..truncatedLength] + "…";
            }

            // MAY ZEUS STRIKE ME DOWN IF THIS IS SHITCOD-
            // *lighting strike*
            // We specifically check here if the message we received was sent by MessageBwoinkManager
            if (message.Flags.HasFlag(MessageFlags.System) && byte.TryParse(message.Sender, out var messageEnumByte))
            {
                switch ((MessageBwoinkManager.BwoinkStatusTypes)messageEnumByte)
                {
                    case MessageBwoinkManager.BwoinkStatusTypes.Banned:
                        messageBuilder.Append(":no_entry: ");
                        break;
                    case MessageBwoinkManager.BwoinkStatusTypes.Disconnect:
                        messageBuilder.Append(":red_circle: ");
                        break;
                    case MessageBwoinkManager.BwoinkStatusTypes.Reconnect:
                        messageBuilder.Append(":green_circle: ");
                        break;

                    default:
                        // Default to manager sent. Throwing here is shitty cause we dont wanna drop relay messages.
                        messageBuilder.Append(":outbox_tray: ");
                        break;
                }
            }
            else if (message.Flags.HasFlag(MessageFlags.Manager))
                messageBuilder.Append(":outbox_tray: ");
            else if(message.Flags.HasFlag(MessageFlags.NoReceivers))
                messageBuilder.Append(":sos: ");
            else
                messageBuilder.Append(":inbox_tray: ");

            messageBuilder.Append(formattedMessage);
            messageBuilder.AppendLine();

            // Would our message exceed the embed *currently*?
            if (descBuilder.Length + messageBuilder.Length > MaxCharsPerEmbed)
            {
                // It does? We send off our embed.
                yield return FinishEmbed(i);
                embed = CreateEmbedFor(userChannel); // Needed since calling any With* affects the returned reference too.

                // Do we already have this message?
                var existingIndex = relayMessages.FindIndex(x => x.LowerBound == startIndex);
                if (existingIndex != -1)
                {
                    relayMessages[existingIndex].MaxBound = i;
                }
                else
                {
                    relayMessages.Add(new DiscordRelayMessage
                    {
                        LowerBound = startIndex,
                        MaxBound = i,
                    });
                }

                // Reset our builders
                descBuilder.Clear();
                descBuilder.Append(messageBuilder);
                messageBuilder.Clear();
                startIndex = i;
            }

            descBuilder.Append(messageBuilder);
            messageBuilder.Clear();
        }

        // Last embed (newest)
        if (descBuilder.Length > 0)
        {
            yield return FinishEmbed(messages.Messages.Count);

            var existingIndex = relayMessages.FindIndex(x => x.LowerBound == startIndex);
            if (existingIndex != -1)
            {
                relayMessages[existingIndex].MaxBound = messages.Messages.Count;
            }
            else
            {
                relayMessages.Add(new DiscordRelayMessage
                {
                    LowerBound = startIndex,
                    MaxBound = messages.Messages.Count,
                });
            }
        }
        yield break;

        EmbedProperties FinishEmbed(int i)
        {
            embed = embed.WithDescription(descBuilder.ToString());
            // The embed color is determined by if any manager has read it.
            // For this, if our last message does not have the "NoReceivers" flag.
            var managerRead = !messages.Messages[i -1].Flags.HasFlag(MessageFlags.NoReceivers);

            return embed.WithColor(new NetCord.Color(managerRead ? 0x41F097 : 0xFF0000));
        }
    }

    private EmbedProperties CreateEmbedFor(NetUserId userId)
    {
        var roundId = -1;
        string? charName = null;
        var session = _playerManager.GetPlayerData(userId);

        // i hate whoever made gameticker an entity system
        // we cry daily because of it.
        // (┬┬﹏┬┬)
        if (_entitySystemManager.TryGetEntitySystem<GameTicker>(out var gameTicker))
        {
            roundId = gameTicker.RoundId;
        }

        if (_entitySystemManager.TryGetEntitySystem<MindSystem>(out var mind))
        {
            charName = mind.GetCharacterName(userId);
        }

        var footer = _localizationManager.GetString("bwoink-discord-relay-footer",
            ("serverName", _configurationManager.GetCVar(CCVars.AdminLogsServerName)),
            ("roundId", roundId));

        var embed = new EmbedProperties()
            .WithFooter(new EmbedFooterProperties().WithText(footer));

        if (charName != null)
        {
            embed = embed.WithTitle(_localizationManager.GetString("bwoink-discord-relay-title",
                ("username", session.UserName),
                ("roundRep", charName)));
        }
        else
        {
            embed = embed.WithTitle(_localizationManager.GetString("bwoink-discord-relay-title-no-rep",
                ("username", session.UserName)));
        }

        return embed;
    }

    private void ReloadedData()
    {
        _channelToChannelMapping.Clear();
        foreach (var channel in _prototypeManager.EnumeratePrototypes<BwoinkChannelPrototype>())
        {
            if (channel.TryGetFeature<DiscordRelay>(out var relay))
            {
                if (ulong.TryParse(_configurationManager.GetCVar<string>(relay.ChannelCvar), out var channelId))
                {
                    _channelToChannelMapping.Add(channel, channelId);
                }
                else
                {
                    _channelToChannelMapping.Add(channel, null);
                }
            }
            else
            {
                _channelToChannelMapping.Add(channel, null);
            }

            _channelQueue.TryAdd(channel, new Queue<NetUserId>());
            _activeMessages.TryAdd(channel, []);
        }

        Log.Info($"Got {_channelToChannelMapping.Values.Count(x => x.HasValue)} channels to relay.");
    }

    private void BwoinkReceived(ProtoId<BwoinkChannelPrototype> sender, (NetUserId person, BwoinkMessage message) args)
    {
        if (!_prototypeManager.Resolve(sender, out var channel))
            return;

        if (!channel.HasFeature<DiscordRelay>() || !_discordLink.IsConnected)
            return;

        if (!_channelQueue.ContainsKey(sender))
            _channelQueue.Add(sender, []);

        if (!_channelQueue[sender].Contains(args.person))
            _channelQueue[sender].Enqueue(args.person);
    }
}

/// <summary>
/// Represents a message sent to Discord.
/// </summary>
public sealed class DiscordRelayMessage
{
    /// <summary>
    /// The message we sent. May be null if it's a very old message.
    /// </summary>
    public RestMessage? Message { get; set; }

    /// <summary>
    /// The index into the conversation
    /// </summary>
    public int LowerBound { get; set; } = 0;

    /// <summary>
    /// The count to index into the conversation.
    /// </summary>
    public int? MaxBound { get; set; } = null;

    /// <summary>
    /// If this message can be considered "done". This means it won't be updated anymore.
    /// </summary>
    public bool Done { get; set; }

    /// <summary>
    /// The message ID. Used for when <see cref="Message"/> is null and <see cref="Done"/> is true.
    /// </summary>
    public ulong? MessageId { get; set; }
}
