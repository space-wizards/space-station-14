using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class OrganComponent : Component
{
    [DataField("body")]
    public EntityUid? Body;

    //is this organ exposed?
    [DataField("exposed")]
    public bool Exposed = true;

    [DataField("parent")]
    public OrganSlot? ParentSlot;
}
