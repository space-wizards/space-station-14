using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
public sealed class OrganComponentState : ComponentState
{
    public OrganSlot? Parent;

    public OrganComponentState(OrganSlot? parent)
    {
        Parent = parent;
    }
}
