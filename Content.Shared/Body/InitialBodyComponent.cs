using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

/// <summary>
/// On map initialization, spawns the given organs into the body.
/// Liable to change as the body becomes more complex.
/// </summary>
[RegisterComponent]
[Access(typeof(InitialBodySystem))]
public sealed partial class InitialBodyComponent : Component
{
    /// <summary>
    /// The organs to spawn based on their category.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntProtoId<OrganComponent>> Organs;
}
