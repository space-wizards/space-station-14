using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Roles.Jobs;

/// <summary>
///     Added to mind entities to hold the data for the player's current job.
/// </summary>
[RegisterComponent]
public sealed partial class JobComponent : Component
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string? PrototypeId;
}
