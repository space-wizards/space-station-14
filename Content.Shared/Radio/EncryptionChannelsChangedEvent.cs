using Content.Server.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class EncryptionChannelsChangedEvent : EntityEventArgs
{
    public readonly EncryptionKeyHolderComponent Component;

    public EncryptionChannelsChangedEvent(EncryptionKeyHolderComponent component)
    {
        Component = component;
    }
}
