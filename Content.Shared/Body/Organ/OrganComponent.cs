using Content.Shared.Body.Systems;
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
    [DataField("internal")]
    public bool Internal = true;

    [ViewVariables]
    [DataField("parent")]
    public OrganSlot? ParentSlot;
}
