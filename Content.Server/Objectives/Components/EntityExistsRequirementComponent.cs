using Content.Server.Objectives.Systems;
using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a certain objective exists for this objective to be given.
/// </summary>
[RegisterComponent, Access(typeof(EntityExistsRequirementSystem))]
public sealed partial class EntityExistsRequirementComponent : Component
{
    /// <summary>
    /// A whitelist for the entity that needs to exist in order for this objective to be assigned.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist;
}
