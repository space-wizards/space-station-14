using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
public sealed partial class OrganComponentState : ComponentState
{
    public readonly EntityUid? Body;
    public readonly OrganSlot? Parent;

    public OrganComponentState(EntityUid? body, OrganSlot? parent)
    {
        Body = body;
        Parent = parent;
    }
}
