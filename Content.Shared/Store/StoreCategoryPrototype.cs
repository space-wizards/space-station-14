using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

/// <summary>
///     Used to define different categories for a store.
/// </summary>
[Prototype("storeCategory")]
[DataDefinition]
public sealed class StoreCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; } = string.Empty;
}
