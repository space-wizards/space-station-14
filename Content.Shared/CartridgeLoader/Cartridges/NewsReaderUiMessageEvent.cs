using Content.Shared.MassMedia.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NewsReaderUiMessageEvent : CartridgeMessageEvent
{
    public readonly NewsReaderUiAction Action;

    public NewsReaderUiMessageEvent(NewsReaderUiAction action)
    {
        Action = action;
    }
}

/// <summary>
/// Sent from the PDA news reader UI when a user toggles a reaction on the current article.
/// </summary>
[Serializable, NetSerializable]
public sealed class NewsReaderReactionMessageEvent : CartridgeMessageEvent
{
    public readonly NewsReactionType Reaction;

    public NewsReaderReactionMessageEvent(NewsReactionType reaction)
    {
        Reaction = reaction;
    }
}

/// <summary>
/// Sent from the PDA news reader UI when a user submits a comment on the current article.
/// </summary>
[Serializable, NetSerializable]
public sealed class NewsReaderCommentMessageEvent : CartridgeMessageEvent
{
    public readonly string Comment;

    public NewsReaderCommentMessageEvent(string comment)
    {
        Comment = comment;
    }
}

[Serializable, NetSerializable]
public enum NewsReaderUiAction
{
    Next,
    Prev,
    NotificationSwitch
}


