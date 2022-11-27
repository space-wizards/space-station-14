using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Wounds.Components;

[Serializable, NetSerializable]
public sealed class WoundComponentState : ComponentState
{
    public EntityUid Parent;

    public WoundComponentState(EntityUid parent)
    {
        Parent = parent;
    }
}
