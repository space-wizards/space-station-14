using Robust.Shared.Network;
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
}
