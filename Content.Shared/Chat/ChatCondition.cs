using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;

namespace Content.Shared.Chat;

[Serializable]
[DataDefinition]
[Virtual]
public sealed partial class AllChatCondition : IChatCondition
{
    /// <inheritdoc />
    public AllChatCondition()
    {
    }

    /// <inheritdoc />
    public AllChatCondition(List<ChatCondition> subconditions)
    {
        Subconditions = subconditions;
    }

    [DataField]
    public List<ChatCondition> Subconditions = new();

    /// <inheritdoc />
    public bool Check(ChatMessageConditionSubject subject, ChatMessageContext chatContext)
    {
        foreach (var chatCondition in Subconditions)
        {
            if (!chatCondition.Check(subject, chatContext))
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
[DataDefinition]
[Virtual]
public sealed partial class AnyChatCondition : IChatCondition
{
    /// <inheritdoc />
    public AnyChatCondition()
    {
    }

    /// <inheritdoc />
    public AnyChatCondition(List<IChatCondition> subconditions)
    {
        Subconditions = subconditions;
    }

    [DataField]
    public List<IChatCondition> Subconditions = new();

    /// <inheritdoc />
    public bool Check(ChatMessageConditionSubject subject, ChatMessageContext chatContext)
    {
        foreach (var chatCondition in Subconditions)
        {
            if (chatCondition.Check(subject, chatContext))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class ChatMessageContext : Dictionary<Enum, object>
{
    /// <inheritdoc />
    public ChatMessageContext(IDictionary<Enum, object> dictionary) : base(dictionary)
    {
    }

    public ChatMessageContext(Dictionary<Enum, object> dictionary, ChatMessageContext? otherContext) : this(dictionary)
    {
        if (otherContext == null)
            return;

        foreach (var (key, value) in otherContext)
        {
            this[key] = value;
        }
    }

    public bool TryGet<T>(Enum key, [NotNullWhen(true)] out T? value)
    {
        if (TryGetValue(key, out var val))
        {
            value = (T)val;
            return true;
        }

        value = default;
        return false;
    }
}

/// <summary>
/// Base class for chat conditions that can handle check only for <see cref="ICommonSession"/>.
/// Should be used for checks that can be run only against session, or out-of sim checks.
/// </summary>
public abstract partial class SessionChatConditionBase : ChatCondition
{
    /// <inheritdoc />
    protected sealed override bool Check(EntityUid subjectEntity, ChatMessageContext chatContext)
    {
        return false;
    }
}

/// <summary>
/// Base class for chat conditions that can handle check only for <see cref="EntityUid"/>.
/// Should be used for checks of all in-sim conditions.
/// </summary>
public abstract partial class EntityChatConditionBase : ChatCondition
{
    /// <inheritdoc />
    protected sealed override bool Check(ICommonSession subjectEntity, ChatMessageContext chatContext)
    {
        return false;
    }
}

/// <summary>
/// Condition that checks if session (player) / entity (game object)
/// can produce or receive chat messages.
/// </summary>
public interface IChatCondition
{
    /// <summary>
    /// Checks if provided subject fits condition. Subject can be <see cref="ICommonSession"/> or
    /// <see cref="EntityUid"/>. In case of session - attached entity will be tested as
    /// <see cref="ChatMessageConditionSubject"/> automatically extracts it.
    /// </summary>
    /// <param name="subject">Subject of check.</param>
    /// <param name="chatContext">Context of chat message publish / consume.</param>
    /// <returns>True if subject entity OR session fits check, false otherwise.</returns>
    bool Check(ChatMessageConditionSubject subject, ChatMessageContext chatContext);
}

[Serializable]
[DataDefinition]
[Virtual]
public abstract partial class ChatCondition : IChatCondition
{
    // If true, invert the result of the condition.
    [DataField]
    public bool Inverted = false;

    /// <inheritdoc />
    public virtual bool Check(
        ChatMessageConditionSubject subject,
        ChatMessageContext chatContext
    )
    {
        if (subject.Entity.HasValue && (Check(subject.Entity.Value, chatContext) == !Inverted))
        {
            return true;
        }

        if (subject.Session != null && (Check(subject.Session, chatContext) == !Inverted))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks condition against entity. Usually used for in-sim checks.
    /// </summary>
    protected abstract bool Check(EntityUid subjectEntity, ChatMessageContext chatContext);

    /// <summary>
    /// Checks condition against session. Usually used for out-of sim, session-only or non-sim dependent checks.
    /// </summary>
    protected abstract bool Check(ICommonSession subjectSession, ChatMessageContext chatContext);

}

public sealed class ChatMessageConditionSubject
{
    public ChatMessageConditionSubject(EntityUid entity)
    {
        Entity = entity;
    }

    public ChatMessageConditionSubject(ICommonSession session)
    {
        Session = session;
        Entity = session.AttachedEntity;
    }

    public EntityUid? Entity { get; }
    public ICommonSession? Session { get; }
}
