using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Requirements;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager
{
    /// <summary>
    /// Event that runs before a message is sent to a (non-manager) client.
    /// </summary>
    /// <remarks>
    /// This also gets invoked for every message during a sync.
    /// If you compute PI in this event handler, consider not doing that unless you want to make joining a server lockup the main thread for years.
    /// </remarks>
    public event Action<BwoinkMessageClientSentEventArgs>? MessageBeingSent;

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
    /// Sends a message into the specified channel. This is basically just a wrapper around <see cref="SynchronizeMessage"/>.
    /// Before using this, consider if your use case is not covered by the other overloads that make the message themselves.
    /// </summary>
    /// <remarks>
    /// This doesn't perform any kind of saftey checks, so like, don't blow shit up with this.
    /// </remarks>
    [PublicAPI]
    public void SendMessageInChannel(ProtoId<BwoinkChannelPrototype> channel,
        NetUserId userChannel,
        BwoinkMessage message)
    {
        SynchronizeMessage(channel,
            userChannel,
            message);
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
}

/// <summary>
/// Event args for when a message is about to be sent to a client.
/// This will only ever get invoked for non-managers
/// </summary>
public sealed class BwoinkMessageClientSentEventArgs
{
    /// <summary>
    /// The message that is being sent.
    /// </summary>
    public BwoinkMessage Message { get; set; }

    /// <summary>
    /// The player we are about to send this to.
    /// </summary>
    public ICommonSession Target { get; private set; }

    public BwoinkMessageClientSentEventArgs(BwoinkMessage message, ICommonSession target)
    {
        Message = message;
        Target = target;
    }
}
