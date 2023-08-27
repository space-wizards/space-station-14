using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Roles.Jobs;

[RegisterComponent]
public sealed partial class JobComponent : Component
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string? PrototypeId;
}
