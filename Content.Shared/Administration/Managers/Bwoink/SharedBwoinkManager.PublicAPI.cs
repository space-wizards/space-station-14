using Content.Shared.Administration.Managers.Bwoink.Features;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Managers.Bwoink;

public abstract partial class SharedBwoinkManager
{
    /// <summary>
    /// Creates a bwoink message for a given channel using the provided sender.
    /// </summary>
    public BwoinkMessage CreateUserMessage(string text, MessageFlags flags, NetUserId sender)
    {
        return new BwoinkMessage(PlayerManager.GetSessionById(sender).Name, sender, DateTime.UtcNow, text, flags);
    }

    public BwoinkMessage CreateUserMessage(string message, NetUserId? sender, string? senderName, MessageFlags flags)
    {
        if (senderName != null)
            return new BwoinkMessage(senderName, sender, DateTime.UtcNow, message, flags);


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

        return new BwoinkMessage(senderName, sender, DateTime.UtcNow, message, flags);
    }

    /// <summary>
    /// Creates a bwoink message for a given channel using the system user.
    /// </summary>
    public BwoinkMessage CreateSystemMessage(string text, MessageFlags flags = MessageFlags.Manager)
    {
        return new BwoinkMessage(LocalizationManager.GetString("bwoink-system-user"),
            null,
            DateTime.UtcNow,
            text,
            flags);
    }

    /// <summary>
    /// Formats a bwoink message into a string that can be displayed in UIs.
    /// </summary>
    public FormattedMessage FormatMessage(ProtoId<BwoinkChannelPrototype> channelId ,BwoinkMessage message)
    {
        var channel = ProtoCache[channelId];

        var formatted = new FormattedMessage();

        var color = Color.White;
        if (message.Flags.HasFlag(MessageFlags.Manager))
            color = Color.Red;

        formatted.PushColor(Color.Gray);
        formatted.AddText($"{message.SentAt.ToShortTimeString()} ");
        formatted.Pop();

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

        formatted.PushColor(color);
        formatted.AddText($"{message.Sender} ");
        formatted.Pop();

        formatted.AddText(message.Content);

        return formatted;
    }
}
