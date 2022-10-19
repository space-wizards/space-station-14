using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class OrganComponent : Component
{
    [ViewVariables] public EntityUid? Body;

    [ViewVariables]
    [DataField("parent")]
    public OrganSlot? ParentSlot;
}
