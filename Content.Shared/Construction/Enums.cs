using Robust.Shared.Serialization;

namespace Content.Shared.Construction;

[Serializable, NetSerializable]
public enum ConstructionVisuals : byte
{
    Key,
    Layer,
    Wired,
}
