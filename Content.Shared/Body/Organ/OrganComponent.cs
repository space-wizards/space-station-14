using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodySystem))]
public sealed class OrganComponent : Component
{
    [DataField("body")]
    [AutoNetworkedField]
    public EntityUid? Body;

    [DataField("parent")]
    [AutoNetworkedField]
    public OrganSlot? ParentSlot;
}
