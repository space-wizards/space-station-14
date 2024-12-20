using Robust.Shared.Serialization;

namespace Content.Shared.ShadowDimension;

public abstract class SharedShadowDimensionSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed record ShadowDimensionParams
{
    public int Seed;
}
