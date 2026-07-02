using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerCartridgeUiMessageEvent : CartridgeMessageEvent
{
    public MessengerCartridgeUiMessageEvent() { }
}
