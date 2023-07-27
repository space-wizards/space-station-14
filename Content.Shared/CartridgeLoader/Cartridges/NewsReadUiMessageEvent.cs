using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NewsReadUiMessageEvent : CartridgeMessageEvent
{
    public readonly NewsReadUiAction Action;

    public NewsReadUiMessageEvent(NewsReadUiAction action)
    {
        Action = action;
    }
}

[Serializable, NetSerializable]
public enum NewsReadUiAction
{
    Next,
    Prev,
    NotificationSwith
}
