using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BodySystem))]
public sealed partial class OrganComponent : Component
{
    /// <summary>
    /// The body entity containing this organ, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// What kind of organ is this, if any
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Category;
}
