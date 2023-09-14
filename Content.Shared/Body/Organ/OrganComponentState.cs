using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
public sealed class OrganComponentState : ComponentState
{
    public readonly NetEntity? Body;
    public readonly OrganSlot? Parent;

    public OrganComponentState(NetEntity? body, OrganSlot? parent)
    {
        Body = body;
        Parent = parent;
    }
}
