using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.EntitySystems;

public abstract partial class SharedEmitterSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public enum EmitterModesUiKey
{
    Key
}

