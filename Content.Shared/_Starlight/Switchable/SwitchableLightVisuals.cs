using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Switchable;

// Appearance Data key
[Serializable, NetSerializable]
public enum SwitchableLightVisuals : byte
{
    Enabled,
    Color
}
