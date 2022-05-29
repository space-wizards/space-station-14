using Robust.Shared.Serialization;

namespace Content.Shared.Toggleable;

// Appearance Data key
[Serializable, NetSerializable]
public enum ToggleableLightVisuals
{
    Enabled,
    Color
}
