using Robust.Shared.Serialization;

namespace Content.Shared.Light;

[Serializable, NetSerializable]
public sealed class ToggleFlashlightEvent : EntityEventArgs
{
    public NetEntity Flashlight;

    public ToggleFlashlightEvent(NetEntity flashlight)
    {
        Flashlight = flashlight;
    }
}
