using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class DigitalIanUiMessageEvent : CartridgeMessageEvent
{
    public readonly DigitalIanUiAction Action;

    public DigitalIanUiMessageEvent(DigitalIanUiAction action)
    {
        Action = action;
    }
}

[Serializable, NetSerializable]
public enum DigitalIanUiAction
{
    Feed,
    Pet
}
