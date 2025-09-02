using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

/// <summary>
/// Requires that the player not have a certain job to have this objective.
/// </summary>
[RegisterComponent, Access(typeof(NotJobRequirementSystem))]
public sealed partial class NotJobRequirementComponent : Component
{
    /// <summary>
    /// ID of the job to ban from having this objective, use for singular job exclusions (i.e. steal tropico for atmos tech).
    /// </summary>
    /// <remarks>
    /// May be worth phasing this out later, but that would be a breaking change.
    /// </remarks>
    [DataField]
    public ProtoId<JobPrototype> Job = string.Empty;

    /// <summary>
    /// List of IDs to ban from having this objective, for multi-job exclusions (i.e. steal paramed's voidsuit for paramedic, chemist, doctor, psychologist)
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = new List<ProtoId<JobPrototype>>();
}
