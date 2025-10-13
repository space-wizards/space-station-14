using System.Linq;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Managers.Bwoink;

public abstract class SharedBwoinkManager : IPostInjectInit
{
    [Dependency] private readonly ILogManager _logManager = default!;

    // Protected members:
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] protected readonly ISharedAdminManager AdminManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    protected ISawmill Log = default!;
    public event EventHandler<ProtoId<BwoinkChannelPrototype>, (NetUserId person, BwoinkMessage message)>? MessageReceived;
    protected Dictionary<ProtoId<BwoinkChannelPrototype>, BwoinkChannelPrototype> ProtoCache = new();

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
    /// Allows inheriting classes (so ClientBwoinkManager and ServerBwoinkManager) to call invoke on <see cref="MessageReceived"/>
    /// This method also adds it to the relevant Conversation in <see cref="Conversations"/>
    /// </summary>
    /// <remarks>
    /// This is needed because only the class that declared an event can invoke that event.
    /// </remarks>
    /// <param name="senderName">The name of the sender to use. If null, the sender user ID will be resolved to a name.</param>
    protected void InvokeMessageReceived(ProtoId<BwoinkChannelPrototype> channel, NetUserId target, string message, NetUserId? sender, string? senderName, MessageFlags flags)
    {
        if (senderName == null)
        {
            DebugTools.AssertNotNull(sender, "sender must not be null when senderName is.");
            if (!sender.HasValue)
            {
                Log.Error("Received null sender with null senderName!");
                return;
            }

            senderName = PlayerManager.GetSessionById(sender.Value).Name;
        }

        var messageObject = new BwoinkMessage(senderName, sender, DateTime.UtcNow, message, flags);

        if (Conversations[channel].TryGetValue(target, out var value))
            value.Messages.Add(messageObject);
        else
        {
            // ReSharper disable once UseCollectionExpression I wish. But this fails sandbox.
            Conversations[channel].Add(target, new Conversation(target, new List<BwoinkMessage>() { messageObject }));
        }

        MessageReceived?.Invoke(channel, (target, messageObject));
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
        UpdatedChannels();
    }

    void IPostInjectInit.PostInject()
    {
        Log = _logManager.GetSawmill("bwoink");
    }

    public bool CanManageChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        var prototype = PrototypeManager.Index(proto);
        return CanManageChannel(prototype, session);
    }

    public bool CanManageChannel(BwoinkChannelPrototype channel, ICommonSession session)
    {
        if (channel.Features.TryFirstOrDefault(bwoinkChannelFeature => bwoinkChannelFeature is RequiredFlags,
                out var feature))
        {
            return AdminManager.HasAdminFlag(session, (feature as RequiredFlags)!.Flags);
        }

        return true;
    }

    /// <summary>
    /// Called once the channels are updated.
    /// </summary>
    protected virtual void UpdatedChannels() { }

    /// <summary>
    /// Holds a history of all of our conversations.
    /// This is synced between client -> server and may periodically be cleared out and re-synced.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, Conversation>> Conversations = new();
}

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
public sealed record BwoinkMessage(string Sender, NetUserId? SenderId, DateTime SentAt, string Content, MessageFlags Flags)
{
    /// <summary>
    /// The sender of this message.
    /// For reasons of (for example) hiding the sender, this is a string of the sender name.
    /// </summary>
    [ViewVariables]
    public string Sender { get; init; } = Sender;

    /// <summary>
    /// The User ID of the sender. This is may be null on the client if the true sender is hidden or the "system".
    /// </summary>
    [ViewVariables]
    public NetUserId? SenderId { get; init; } = SenderId;

    /// <summary>
    /// The time (in utc) when this message was sent.
    /// </summary>
    [ViewVariables]
    public DateTime SentAt { get; init; } = SentAt;

    /// <summary>
    /// The contents of this message.
    /// </summary>
    [ViewVariables]
    public string Content { get; init; } = Content;

    [ViewVariables]
    public MessageFlags Flags { get; init; } = Flags;
}

public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs args);

[Flags]
public enum MessageFlags
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
}
