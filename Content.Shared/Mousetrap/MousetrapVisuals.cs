using Robust.Shared.Serialization;

namespace Content.Shared.Mousetrap;

[Serializable, NetSerializable]
public enum MousetrapVisuals : byte
{
    Visual,
    Armed,
    Unarmed
}
