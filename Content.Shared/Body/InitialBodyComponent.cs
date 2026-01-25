using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

/// <summary>
/// On map initialization, spawns the given organs into the body.
/// </summary>
[RegisterComponent]
[Access(typeof(InitialBodySystem))]
public sealed partial class InitialBodyComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntProtoId<OrganComponent>> Organs;
}
