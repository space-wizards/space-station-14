using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

/// <summary>
/// Requires that the player not have a certain job to have this objective.
/// </summary>
[RegisterComponent, Access(typeof(NotJobRequirementSystem))]
public sealed partial class NotJobRequirementComponent : Component
{
    /// <summary>
    /// ID of the job to ban from having this objective.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string Job = string.Empty;
}
