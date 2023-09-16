using Content.Shared.Body.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodySystem))]
public sealed partial class OrganComponent : Component
{
    [DataField("body"), AutoNetworkedField]
    public EntityUid? Body;

    [ViewVariables] public string? AttachedToSlot;

    [AutoNetworkedField, ViewVariables] public EntityUid? Parent;

}
