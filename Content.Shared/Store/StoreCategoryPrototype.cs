using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Store;

/// <summary>
///     Used to define different categories for a store.
/// </summary>
[Prototype("storeCategory")]
[Serializable, NetSerializable, DataDefinition]
public sealed class StoreCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; } = string.Empty;

    [DataField("priority")]
    public int Priority { get; } = 0;
}
