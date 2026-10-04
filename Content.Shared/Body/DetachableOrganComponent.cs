using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

/// <summary>
/// An organ that can be detached with all of its children into a new body
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DetachableOrganSystem))]
public sealed partial class DetachableOrganComponent : Component
{
    /// <summary>
    /// The body to spawn upon detaching
    /// </summary>
    [DataField(required: true)]
    public EntProtoId DetachedBody;
}
