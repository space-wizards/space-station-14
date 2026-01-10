using Content.Shared.EntityConditions.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions;

/// <summary>
/// A prototype for entity conditions which can be reused via <see cref="NestedCondition"/>.
/// </summary>
[Prototype]
public sealed partial class EntityConditionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// The condition of this prototype.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition Condition = default!;
}
