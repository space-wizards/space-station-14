using System.Linq;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration.Managers.Bwoink;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Managers.Bwoink;

/// <summary>
/// Handles the server side APIs for bwoinking.
/// </summary>
/// <seealso cref="SharedBwoinkManager"/>
public sealed partial class ServerBwoinkManager : SharedBwoinkManager
{
    private const string RateLimitKey = "AdminHelp";

    /// <summary>
    /// The amount of time required for a person to be no longer typing.
    /// </summary>
    private static readonly TimeSpan TypingTimeout = TimeSpan.FromSeconds(3);
    /// <summary>
    /// Simple rate-limit for the update loop. Since we are doing a LINQ call with RemoveAll, this is a certified hot-loop rider screamed at me for.
    /// </summary>
    private static readonly TimeSpan UpdateTimeout = TimeSpan.FromMilliseconds(500);
    /// <summary>
    /// When we are next going to update the typing timeouts.
    /// </summary>
    private TimeSpan _nextUpdate = TimeSpan.MinValue;

    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimitManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeMessages();
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    /// <summary>
    /// Validates that the protoId given is actually a real prototype. Needed since clients can just send whatever as the ID.
    /// </summary>
    /// <remarks>
    /// get real
    /// </remarks>
    private bool IsPrototypeReal(ProtoId<BwoinkChannelPrototype> channel)
    {
        // If this fails, Resolve will log an error.
        return PrototypeManager.Resolve<BwoinkChannelPrototype>(channel, out _);
    }

    public void Update()
    {
        if (_gameTiming.RealTime < _nextUpdate)
            return;

        _nextUpdate = _gameTiming.RealTime + UpdateTimeout;

        foreach (var (channelId, typings) in TypingStatuses)
        {
            var requireRefresh = false;
            foreach (var (_, statusList) in typings)
            {
                var amongusVent = statusList.RemoveAll(x => _gameTiming.RealTime > x.Timeout) > 0;
                if (amongusVent && !requireRefresh)
                    requireRefresh = true;
            }

            if (requireRefresh)
                SynchronizeTyping(channelId);
        }
    }

    /// <summary>
    /// Signals a user to be "typing" in a channel.
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="userChannel">The user channel they are typing in. This usually is the same as the typing user.</param>
    /// <param name="userTyping">The user who is typing.</param>
    /// <param name="typing">Is the user currently typing?</param>
    /// <remarks>
    /// If we are already typing, this method refreshes the typing status.
    /// </remarks>
    public void SetTypingStatus(ProtoId<BwoinkChannelPrototype> channel, NetUserId userChannel, NetUserId userTyping, bool typing)
    {
        var statuses = GetTypingStatuses(channel, userChannel);

        var index = statuses.FindIndex(x => x.TypingUser == userTyping);
        if (index != -1)
        {
            if (typing)
            {
                // Refresh status
                var updated = statuses[index] with { Timeout = _gameTiming.RealTime + TypingTimeout };
                statuses[index] = updated;
                // No need to sync again.
            }
            else
            {
                // Nuke the status
                statuses.RemoveAt(index);
                SynchronizeTyping(channel);
            }
        }
        else
        {
            statuses.Add(new TypingStatus(userTyping, _gameTiming.RealTime + TypingTimeout, PlayerManager.GetSessionById(userTyping).Name));
            SynchronizeTyping(channel);
        }
    }

    /// <summary>
    /// Sends all currently typing players to each manager for the specified channel.
    /// </summary>
    public void SynchronizeTyping(ProtoId<BwoinkChannelPrototype> channel)
    {
        var message = new MsgBwoinkTypings
        {
            Channel = channel,
            Typings = TypingStatuses[channel],
        };

        var receivers = PlayerManager.Sessions
            .Where(x => CanManageChannel(channel, x))
            .Select(x => x.Channel)
            .ToList();

        _netManager.ServerSendToMany(message, receivers);
    }

    /// <summary>
    /// Re-sends the conversations for a session. This respects manager permissions and such.
    /// </summary>
    public void SynchronizeMessages(ICommonSession session)
    {
        var conversations = new Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, Conversation>>();

        foreach (var (channelId, channel) in ProtoCache)
        {
            if (!CanReadChannel(channel, session))
            {
                // Can't even read it. Smh.
                conversations.Add(channelId, []);
                continue;
            }

            if (CanManageChannel(channel, session))
            {
                // This person is a manager, so we send them all the conversations for this channel.

                conversations.Add(channelId, GetConversationsForChannel(channelId));
            }
            else
            {
                // Not a manager, you get 1984'd messages that are only related to you.

                var convo = GetFilteredConversation(session.UserId, channelId, true);
                if (convo != null)
                {
                    // this is kind of shitcode, but we raise MessageBeingSent on a ref for every message inside the conversation.
                    for (var index = 0; index < convo.Messages.Count; index++)
                    {
                        var bwoinkMessage = convo.Messages[index];
                        var eventArgs = new BwoinkMessageClientSentEventArgs(bwoinkMessage, session);
                        MessageBeingSent?.Invoke(eventArgs);
                        // This in theory is ass, but I don't assume it is actually an issue.
                        convo.Messages[index] = eventArgs.Message; // please dont hurt me
                    }
                }
                conversations.Add(channelId,
                    convo != null
                        ? new Dictionary<NetUserId, Conversation> { { session.UserId, convo } }
                        : []);
            }
        }

        _netManager.ServerSendMessage(new MsgBwoinkSync()
        {
            Conversations = conversations
        }, session.Channel);
    }
}
