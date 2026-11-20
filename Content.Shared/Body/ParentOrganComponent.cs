using Robust.Shared.GameStates;

namespace Content.Shared.Body;

/// <summary>
/// An organ that can be a parent to other organs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrganRelationSystem))]
public sealed partial class ParentOrganComponent : Component
{
    /// <summary>
    /// The current set of child organs parented to this one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Children = new();
}
