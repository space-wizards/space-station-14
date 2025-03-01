using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Network;
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

    public List<ChatCondition> Subconditions = new();

    /// <inheritdoc />
    public bool Check(ChatMessageConditionSubject subject, ChatMessageContext channelParameters)
    {
        foreach (var chatCondition in Subconditions)
        {
            if (!chatCondition.Check(subject, channelParameters))
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

    public List<IChatCondition> Subconditions = new();

    /// <inheritdoc />
    public bool Check(ChatMessageConditionSubject subject, ChatMessageContext channelParameters)
    {
        foreach (var chatCondition in Subconditions)
        {
            if (chatCondition.Check(subject, channelParameters))
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

public abstract partial class SessionChatConditionBase : ChatCondition
{
    /// <inheritdoc />
    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        return false;
    }
}

public abstract partial class EntityChatConditionBase : ChatCondition
{
    /// <inheritdoc />
    protected override bool Check(ICommonSession subjectEntity, ChatMessageContext channelParameters)
    {
        return false;
    }
}


public interface IChatCondition
{
    bool Check(ChatMessageConditionSubject subject, ChatMessageContext channelParameters);
}

[Serializable]
[DataDefinition]
[Virtual]
public abstract partial class ChatCondition : IChatCondition
{
    // If true, invert the result of the condition.
    [DataField]
    public bool Inverted = false;

    public virtual bool Check(
        ChatMessageConditionSubject subject,
        ChatMessageContext channelParameters
    )
    {
        if (subject.Entity.HasValue && Check(subject.Entity.Value, channelParameters))
        {
            return true;
        }

        if (subject.Session != null && Check(subject.Session, channelParameters))
        {
            return true;
        }

        return false;
    }

    protected abstract bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters);
    protected abstract bool Check(ICommonSession subjectSession, ChatMessageContext channelParameters);

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
