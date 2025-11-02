using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Requirements;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager
{
    /// <summary>
    /// Sends a message to a given channel and user channel using the system user.
    /// </summary>
    [PublicAPI]
    public void SendMessageInChannel(ProtoId<BwoinkChannelPrototype> channel, NetUserId userChannel, string text, MessageFlags flags)
    {
        SynchronizeMessage(channel,
            userChannel,
            CreateSystemMessage(text, flags));
    }

    /// <summary>
    /// Sends a message to a given channel and user channel using the provided sender.
    /// </summary>
    [PublicAPI]
    public void SendMessageInChannel(ProtoId<BwoinkChannelPrototype> channel,
        NetUserId userChannel,
        string text,
        MessageFlags flags,
        NetUserId sender)
    {
        SynchronizeMessage(channel,
            userChannel,
            CreateUserMessage(text, flags, sender));
    }

    /// <summary>
    /// Sets the allow list status of a person. Allow lists are used when for the <see cref="ListRequirement"/>.
    /// </summary>
    [PublicAPI]
    public void SetAllowList(ProtoId<BwoinkChannelPrototype> channel, NetUserId target, bool allow)
    {
        var refresh = false;

        if (_channelAllowList.TryGetValue(channel, out var list))
        {
            if (!allow)
            {
                list.Remove(target);
                refresh = true;
            }
            else if (!list.Contains(target))
            {
                list.Add(target);
                refresh = true;
            }
        }
        else
        {
            _channelAllowList.Add(channel, [target]);
            refresh = true;
        }

        if (refresh)
        {
            var session = PlayerManager.GetSessionById(target);
            SyncChannels(session);
            SynchronizeMessages(session);
        }
    }

    /// <summary>
    /// Creates a bwoink message for a given sender.
    /// </summary>
    private BwoinkMessage CreateUserMessage(string text, MessageFlags flags, NetUserId sender)
    {
        return new BwoinkMessage(PlayerManager.GetSessionById(sender).Name, sender, DateTime.UtcNow, text, flags);
    }

    private BwoinkMessage CreateSystemMessage(string text, MessageFlags flags = MessageFlags.Manager)
    {
        return new BwoinkMessage(_localizationManager.GetString("bwoink-system-user"), null, DateTime.UtcNow, text, flags);
    }
}
