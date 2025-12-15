using System.Linq;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Managers.Bwoink;

/// <summary>
/// This class is responsible for managing the admin help system. See the Server and Client implementation for details.
/// </summary>
public abstract partial class SharedBwoinkManager : IPostInjectInit
{
    [Dependency] private readonly ILogManager _logManager = default!;

    // Protected members:
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] protected readonly ILocalizationManager LocalizationManager = default!;
    [Dependency] protected readonly ISharedAdminManager AdminManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;

    protected ISawmill Log = default!;
    /// <summary>
    /// Event that gets fired for every message that gets sent. The sender is the channel this message was received in.
    /// </summary>
    public event EventHandler<ProtoId<BwoinkChannelPrototype>, (NetUserId person, BwoinkMessage message)>? MessageReceived;
    /// <summary>
    /// Called whenever our prototypes change, or a full state update is applied.
    /// </summary>
    public event Action? ReloadedData;

    /// <summary>
    /// A cache of ProtoIds for the channels resolving to a cached copy of the prototype behind it.
    /// </summary>
    protected Dictionary<ProtoId<BwoinkChannelPrototype>, BwoinkChannelPrototype> ProtoCache = new();

    /// <summary>
    /// This variable holds the people who are currently typing.
    /// The internal dictionary holds the list of "user channels". Typing status itself has the key TypingUser which is the person who is typing.
    /// </summary>
    /// <remarks>
    /// On the client this will only be filled for channels which you can manage.
    /// The server obviously has the full list.
    /// </remarks>
    [ViewVariables]
    protected readonly Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, List<TypingStatus>>> TypingStatuses = new();

    [MustCallBase]
    public virtual void Initialize()
    {
        PrototypeManager.PrototypesReloaded += RefreshChannels;

        // PrototypesReloaded doesn't fire upon first load, so we refresh our channels on startup.
        RefreshChannels();
    }

    [MustCallBase]
    public virtual void Shutdown() // the bwoinkmanager is very eepy sleepy
    {
        PrototypeManager.PrototypesReloaded -= RefreshChannels;
    }

    /// <summary>
    /// Gets the typing statutes for a given channel and user channel.
    /// The returned status list can be modified to affect what is stored in the <see cref="TypingStatuses"/> dict.
    /// </summary>
    public List<TypingStatus> GetTypingStatuses(ProtoId<BwoinkChannelPrototype> channel, NetUserId userChannel)
    {
        // Ensure outer dictionary entry exists
        if (!TypingStatuses.TryGetValue(channel, out var channelDict))
        {
            channelDict = new Dictionary<NetUserId, List<TypingStatus>>();
            TypingStatuses[channel] = channelDict;
        }

        // Ensure inner list exists
        if (!channelDict.TryGetValue(userChannel, out var list))
        {
            list = new List<TypingStatus>();
            channelDict[userChannel] = list;
        }

        return list;
    }

    /// <summary>
    /// Allows inheriting classes (so ClientBwoinkManager and ServerBwoinkManager) to call invoke on <see cref="MessageReceived"/>
    /// This method also adds it to the relevant Conversation in <see cref="Conversations"/>
    /// </summary>
    /// <remarks>
    /// This is needed because only the class that declared an event can invoke that event.
    /// </remarks>
    protected void InvokeMessageReceived(ProtoId<BwoinkChannelPrototype> channel, NetUserId target, BwoinkMessage message)
    {
        if (Conversations[channel].TryGetValue(target, out var value))
            value.Messages.Add(message);
        else
        {
            // ReSharper disable once UseCollectionExpression I wish. But this fails sandbox.
            Conversations[channel].Add(target, new Conversation(target, new List<BwoinkMessage>() { message }));
        }

        MessageReceived?.Invoke(channel, (target, message));
    }

    /// <summary>
    /// Proxy method that calls Invoke on <see cref="ReloadedData"/>
    /// </summary>
    protected void InvokeReloadedData()
    {
        ReloadedData?.Invoke();
    }

    private void RefreshChannels(PrototypesReloadedEventArgs obj)
        => RefreshChannels();

    private void RefreshChannels()
    {
        Log.Info("Refreshing channels...");
        var protos = PrototypeManager.EnumeratePrototypes<BwoinkChannelPrototype>().ToList();
        ProtoCache = protos.ToDictionary(x => new ProtoId<BwoinkChannelPrototype>(x.ID), x => x);

        var conversationsToRemove = Conversations
            .Where(pair => protos.All(proto => proto.ID != pair.Key))
            .ToList();

        conversationsToRemove
            .ToList()
            .ForEach(x => Conversations.Remove(x.Key));

        Log.Debug($"Removed {conversationsToRemove.Count} conversations");

        var conversationsToAdd =
            protos.Where(proto => Conversations.All(x => x.Key != proto.ID))
                .ToList();

        conversationsToAdd
            .ToList()
            .ForEach(x => Conversations.Add(x.ID, new Dictionary<NetUserId, Conversation>()));

        Log.Debug($"Added {conversationsToAdd.Count} conversations");

        // Yeah, this nukes all current typing statutes, but I don't think preserving the statutes in-between reloads is that needed.
        TypingStatuses.Clear();
        foreach (var bwoinkChannelPrototype in protos)
        {
            TypingStatuses.Add(bwoinkChannelPrototype, new Dictionary<NetUserId, List<TypingStatus>>());
        }

        UpdatedChannels();
        ReloadedData?.Invoke();
    }

    void IPostInjectInit.PostInject()
    {
        Log = _logManager.GetSawmill("bwoink");
    }

    /// <summary>
    /// Called once the channels are updated.
    /// </summary>
    protected virtual void UpdatedChannels() { }

    protected (TimeSpan roundTime, int roundId) GetRoundIdAndTime()
    {
        if (!EntitySystemManager.TryGetEntitySystem<SharedGameTicker>(out var gameTicker))
            return (TimeSpan.MinValue, -1);

        return (gameTicker.RoundDuration(), gameTicker.RoundId);
    }

    /// <summary>
    /// Holds a history of all of our conversations.
    /// This is synced between client -> server and may periodically be cleared out and re-synced.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, Conversation>> Conversations = new();
}

/// <summary>
/// Represents a conversation.
/// </summary>
/// <param name="Who">Who is this conversation about. This is referred to as the UserChannel in some places.</param>
/// <param name="Messages">A list of messages in this channel.</param>
public sealed record Conversation(NetUserId Who, List<BwoinkMessage> Messages)
{
    /// <summary>
    /// The person this conversation relates to
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public NetUserId Who { get; init; } = Who;

    /// <summary>
    /// The messages inside this conversation
    /// </summary>
    [ViewVariables]
    public List<BwoinkMessage> Messages { get; init; } = Messages;
}

/// <summary>
/// Represents a message sent into an ahelp channel.
/// </summary>
public sealed record BwoinkMessage
{
    /// <summary>
    /// Represents a message sent into an ahelp channel.
    /// </summary>
    public BwoinkMessage(string sender,
        NetUserId? senderId,
        DateTime sentAt,
        string content,
        MessageFlags flags,
        TimeSpan roundTime,
        int roundId,
        Color? color = null)
    {
        Color = color;
        Sender = sender;
        SenderId = senderId;
        SentAt = sentAt;
        RoundTime = roundTime;
        RoundId = roundId;
        Content = content;
        Flags = flags;
    }

    /// <summary>
    /// The sender of this message.
    /// For reasons of (for example) hiding the sender, this is a string of the sender name.
    /// </summary>
#if DEBUG
    [ViewVariables(VVAccess.ReadWrite)]
#else
    [ViewVariables]
#endif
    public string Sender { get; init; }

    /// <summary>
    /// The User ID of the sender. This is may be null on the client if the true sender is hidden or the "system".
    /// </summary>
    [ViewVariables]
    public NetUserId? SenderId { get; set; }

    /// <summary>
    /// The time (in utc) when this message was sent.
    /// </summary>
    [ViewVariables]
    public DateTime SentAt { get; init; }

    /// <summary>
    /// The time elapsed into the round when this message was sent.
    /// </summary>
    [ViewVariables]
    public TimeSpan RoundTime { get; init; }
    /// <summary>
    /// The round ID when this message was sent, crazy.
    /// </summary>
    [ViewVariables]
    public int RoundId { get; init; }

    /// <summary>
    /// The contents of this message.
    /// </summary>
#if DEBUG
    [ViewVariables(VVAccess.ReadWrite)]
#else
    [ViewVariables]
#endif
    public string Content { get; init; }

    /// <summary>
    /// The flags this message has.
    /// </summary>
#if DEBUG
    [ViewVariables(VVAccess.ReadWrite)]
#else
    [ViewVariables]
#endif
    public MessageFlags Flags { get; set; }

    [ViewVariables]
    public Color? Color { get; set; }
}

public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs args);

/// <summary>
/// Flags that a <see cref="BwoinkMessage"/> can have.
/// </summary>
[Flags]
public enum MessageFlags : byte
{
    None = 0,

    /// <summary>
    /// The person who sent this message is a manager for this channel.
    /// </summary>
    Manager = 1,

    /// <summary>
    /// This message should not result in a sound.
    /// </summary>
    Silent = 2,

    /// <summary>
    /// This message can only be seen by other managers of this channel.
    /// </summary>
    ManagerOnly = 4,

    /// <summary>
    /// No manager received this specific message.
    /// </summary>
    NoReceivers = 8,

    /// <summary>
    /// This message was sent by the server itself, the message sender will not be shown in the UI and may be used for other purposes.
    /// </summary>
    System = 16,

    /// <summary>
    /// Template for your average system message.
    /// </summary>
    GenericSystem = Manager | Silent | System,
}

/// <summary>
/// Represents a person who is typing.
/// </summary>
/// <param name="TypingUser">The person who is typing. Crazy stuff.</param>
/// <param name="Timeout">Time until this status gets cleared.</param>
/// <param name="Username">The username of the typing person.</param>
public record TypingStatus(NetUserId TypingUser, TimeSpan Timeout, string Username)
{
    /// <summary>
    /// The person who is typing. Crazy stuff.
    /// </summary>
    [ViewVariables]
    public NetUserId TypingUser { get; init; } = TypingUser;

    /// <summary>
    /// Time until this status gets cleared.
    /// </summary>
    [ViewVariables]
    public TimeSpan Timeout { get; init; } = Timeout;

    /// <summary>
    /// The username of the typing person.
    /// </summary>
    [ViewVariables]
    public string Username { get; init; } = Username;
}
