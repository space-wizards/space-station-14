using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles;

public abstract partial class AntagonistRoleComponent : Component
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string? PrototypeId;
}

/// <summary>
/// Mark the antagonist role component as being exclusive
/// IE by default other antagonists should refuse to select the same entity for a different antag role
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(AntagonistRoleComponent))]
public sealed partial class ExclusiveAntagonistAttribute : Attribute
{
}
