using Robust.Shared.GameStates;

namespace Content.Shared.Body;

/// <summary>
/// An organ that can be a child to a parent organ within a body.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrganRelationSystem))]
public sealed partial class ChildOrganComponent : Component
{
    /// <summary>
    /// The current organ that's a parent to this one, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Parent;
}
