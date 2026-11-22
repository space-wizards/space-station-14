using Content.Shared.CartridgeLoader;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class OrderLogUiMessageEvent : CartridgeMessageEvent
{
    public readonly OrderLogUiAction Action;

    public OrderLogUiMessageEvent(OrderLogUiAction action)
    {
        Action = action;
    }
}

[Serializable, NetSerializable]
public enum OrderLogUiAction
{
    NotificationSwitch
}
