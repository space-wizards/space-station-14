using Content.Shared.Radio.Components;

namespace Content.Shared.Radio;

public sealed partial class EncryptionChannelsChangedEvent : EntityEventArgs
{
    public readonly EncryptionKeyHolderComponent Component;

    public EncryptionChannelsChangedEvent(EncryptionKeyHolderComponent component)
    {
        Component = component;
    }
}

