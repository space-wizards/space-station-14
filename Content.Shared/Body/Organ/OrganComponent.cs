using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent]
public sealed class OrganComponent : Component
{
    [ViewVariables]
    [DataField("parent")]
    public OrganSlot? ParentSlot;
}
