using Robust.Shared.Serialization;

namespace Content.Shared.EntityFlags.Components;

[Serializable, NetSerializable]
public sealed class EntityFlagComponentState : ComponentState
{
    public HashSet<string> Flags;

    public EntityFlagComponentState(HashSet<string> flags)
    {
        Flags = flags;
    }
}
