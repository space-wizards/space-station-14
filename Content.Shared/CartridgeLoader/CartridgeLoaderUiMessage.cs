using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader;

[Serializable, NetSerializable]
public sealed class CartridgeLoaderUiMessage : BoundUserInterfaceMessage
{
    public readonly EntityUid CartridgeUid;
    public readonly CartridgeUiMessageAction Action;

    public CartridgeLoaderUiMessage(EntityUid cartridgeUid, CartridgeUiMessageAction action)
    {
        CartridgeUid = cartridgeUid;
        Action = action;
    }
}

[Serializable, NetSerializable]
public enum CartridgeUiMessageAction
{
    Activate,
    Deactivate,
    Install,
    Uninstall,
    UIReady
}
