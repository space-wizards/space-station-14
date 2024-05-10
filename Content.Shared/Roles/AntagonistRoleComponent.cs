using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles;

public interface IAntagonistRoleComponent
{
    public ProtoId<AntagPrototype>? PrototypeId { get; set; }

    public string? Briefing { get; set; }
}

/// <summary>
/// Mark the antagonist role component as being exclusive
/// IE by default other antagonists should refuse to select the same entity for a different antag role
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IAntagonistRoleComponent))]
public sealed partial class ExclusiveAntagonistAttribute : Attribute
{
}
