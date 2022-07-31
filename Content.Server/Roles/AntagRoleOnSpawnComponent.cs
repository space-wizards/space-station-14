using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Roles;

[RegisterComponent]
public sealed class AddAntagRoleComponent : Component
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string Prototype = string.Empty;
}
