using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Skeletons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrganRelationSystem))]
public sealed partial class ChildOrganComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Parent;
}
