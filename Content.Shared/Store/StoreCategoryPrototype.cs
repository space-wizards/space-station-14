using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Store;

/// <summary>
///     Used to define different categories for a store.
/// </summary>
[Prototype]
[Serializable, NetSerializable, DataDefinition]
public sealed partial class StoreCategoryPrototype : IPrototype
{
    private string _name = string.Empty;

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = "";

    [DataField("priority")]
    public int Priority { get; private set; } = 0;
}
