using System.Linq;
using Content.Shared.Administration.Managers.Bwoink.Features;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Managers.Bwoink;

public abstract partial class SharedBwoinkManager
{
    /// <summary>
    /// Creates a bwoink message for a given channel using the provided sender.
    /// </summary>
    [PublicAPI]
    public BwoinkMessage CreateUserMessage(string text, MessageFlags flags, NetUserId sender)
    {
        var tickerNonsense = GetRoundIdAndTime();
        return new BwoinkMessage(PlayerManager.GetSessionById(sender).Name, sender, DateTime.UtcNow, text, flags, tickerNonsense.roundTime, tickerNonsense.roundId);
    }

    [PublicAPI]
    public BwoinkMessage CreateUserMessage(string message, NetUserId? sender, string? senderName, MessageFlags flags)
    {
        var tickerNonsense = GetRoundIdAndTime();

        if (senderName != null)
            return new BwoinkMessage(senderName, sender, DateTime.UtcNow, message, flags, tickerNonsense.roundTime, tickerNonsense.roundId);


        DebugTools.AssertNotNull(sender, "sender must not be null when senderName is.");
        if (!sender.HasValue)
        {
            Log.Error("Received null sender with null senderName!");
            senderName = "USER ERROR";
        }
        else
        {
            senderName = PlayerManager.GetSessionById(sender.Value).Name;
        }

        return new BwoinkMessage(senderName, sender, DateTime.UtcNow, message, flags, tickerNonsense.roundTime, tickerNonsense.roundId);
    }

    /// <summary>
    /// Creates a bwoink message for a given channel using the system user.
    /// </summary>
    [PublicAPI]
    public BwoinkMessage CreateSystemMessage(string text, MessageFlags flags = MessageFlags.System)
    {
        var tickerNonsense = GetRoundIdAndTime();

        return new BwoinkMessage(LocalizationManager.GetString("bwoink-system-user"),
            null,
            DateTime.UtcNow,
            text,
            flags,
            tickerNonsense.roundTime,
            tickerNonsense.roundId);
    }

    /// <summary>
    /// Returns all the bwoink channels that have the feature T.
    /// </summary>
    /// <typeparam name="T">The feature.</typeparam>
    [PublicAPI]
    public IEnumerable<(ProtoId<BwoinkChannelPrototype> channel, T feature)> GetBwoinkChannelsWithFeature<T>() where T : BwoinkChannelFeature
    {
        foreach (var (id, prototype) in ProtoCache)
        {
            if (!prototype.TryGetFeature<T>(out var feature))
                continue;

            yield return (id, feature);
        }
    }

    /// <summary>
    /// Gets the conversation for the specified user and channel.
    /// </summary>
    /// <param name="userId">The user you are filtering for.</param>
    /// <param name="channel">The channel you are filtering for.</param>
    /// <param name="filterSender">If the sender should be hidden.</param>
    /// <returns>The conversation, if the user has no conversation for this channel, returns null.</returns>
    [PublicAPI]
    public Conversation? GetFilteredConversation(
        NetUserId userId,
        ProtoId<BwoinkChannelPrototype> channel,
        bool filterSender)
    {
        if (!Conversations.TryGetValue(channel, out var conversations))
        {
            DebugTools.Assert($"Conversations for key {channel.Id} not found.");
            Log.Error($"Conversations for key {channel.Id} not found.");
            return null;
        }

        if (!conversations.TryGetValue(userId, out var conversation))
            return null;

        var filteredMessages = conversation.Messages
            .Where(x => !x.Flags.HasFlag(MessageFlags.ManagerOnly))
            .Select(x => x with { SenderId = filterSender ? null : x.SenderId })
            .ToList();

        return conversation with { Messages = filteredMessages };
    }

    /// <summary>
    /// Returns the conversations for a given channel.
    /// </summary>
    public Dictionary<NetUserId, Conversation> GetConversationsForChannel(ProtoId<BwoinkChannelPrototype> channel)
    {
        return Conversations[channel];
    }

    /// <summary>
    /// Formats a bwoink message into a string that can be displayed in UIs.
    /// </summary>
    [PublicAPI]
    public FormattedMessage FormatMessage(ProtoId<BwoinkChannelPrototype> channelId, BwoinkMessage message, bool useRoundTime = false)
    {
        var channel = ProtoCache[channelId];

        var formatted = new FormattedMessage();

        if (message.Color.HasValue)
            formatted.PushColor(message.Color.Value);

        formatted.PushTag(new MarkupNode("bold", null, null));
        formatted.PushColor(Color.Gray);

        if (useRoundTime)
            formatted.AddText($@"{message.RoundTime:hh\:mm\:ss} ");
        else
            formatted.AddText($"{message.SentAt.ToShortTimeString()} ");

        formatted.Pop();

        // We only need to decorate the message if its not a system message.
        if (!message.Flags.HasFlag(MessageFlags.System))
        {
            if (message.Flags.HasFlag(MessageFlags.ManagerOnly))
            {
                formatted.AddText(channel.TryGetFeature<ManagerOnlyMessages>(out var managerOnlyMessages)
                    ? $"{LocalizationManager.GetString(managerOnlyMessages.Prefix)} "
                    : $"{LocalizationManager.GetString("bwoink-message-manager-only")} ");
            }

            if (message.Flags.HasFlag(MessageFlags.Silent))
            {
                formatted.AddText($"{LocalizationManager.GetString("bwoink-message-silent")} ");
            }

            var color = Color.White;
            if (message.Flags.HasFlag(MessageFlags.Manager))
                color = Color.Red;

            formatted.PushColor(color);
            formatted.AddText($"{message.Sender}: ");
            formatted.Pop();
        }

        formatted.Pop();

        formatted.AddText(message.Content);
        if (message.Color.HasValue)
            formatted.Pop();

        return formatted;
    }
}
