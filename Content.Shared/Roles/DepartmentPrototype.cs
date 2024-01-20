using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Roles;

[Prototype("department")]
public sealed partial class DepartmentPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    ///     A description string to display in the character menu as an explanation of the department's function.
    /// </summary>
    [DataField("description", required: true)]
    public string Description = default!;

    /// <summary>
    ///     A color representing this department to use for text.
    /// </summary>
    [DataField("color", required: true)]
    public Color Color = default!;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("roles", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string> Roles = new();

    /// <summary>
    /// Whether this is a primary department or not.
    /// For example, CE's primary department is engineering since Command has primary: false.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Primary = true;
}
