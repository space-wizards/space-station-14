using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Skeletons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrganRelationSystem))]
public sealed partial class ParentOrganComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Children = new();
}
