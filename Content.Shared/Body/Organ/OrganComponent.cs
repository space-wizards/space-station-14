using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class OrganComponent : Component
{
    [ViewVariables]
    [DataField("body")]
    public EntityUid? Body;

    //is this organ exposed?
    [ViewVariables]
    [DataField("exposed")]
    public bool Exposed = true;

    [ViewVariables]
    [DataField("parent")]
    public OrganSlot? ParentSlot;
}
