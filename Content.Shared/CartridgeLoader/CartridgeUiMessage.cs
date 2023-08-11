using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader;

[Serializable, NetSerializable]
public sealed class CartridgeUiMessage : BoundUserInterfaceMessage
{
    public CartridgeMessageEvent MessageEvent;

    public CartridgeUiMessage(CartridgeMessageEvent messageEvent)
    {
        MessageEvent = messageEvent;
    }
}

[Serializable, NetSerializable]
public abstract class CartridgeMessageEvent : EntityEventArgs
{
    public NetEntity LoaderUid;
}
