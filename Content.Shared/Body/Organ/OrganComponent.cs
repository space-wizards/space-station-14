using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class OrganComponent : Component
{
    [DataField("body")]
    public EntityUid? Body;

    // TODO use containers. See comments in BodyPartComponent.
    // Do not rely on this in client-side code.
    public OrganSlot? ParentSlot;
}
