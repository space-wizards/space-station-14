using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Store;

/// <summary>
///     Prototype used to define different types of currency for generic stores.
///     Mainly used for antags, such as traitors, nukies, and revenants
///     This is separate to the cargo ordering system.
/// </summary>
[Prototype("currency")]
[DataDefinition]
public sealed class CurrencyPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; } = string.Empty;

    [DataField("entityId", serverOnly: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EntityId { get; } = null;

    [DataField("canWithdraw")]
    public bool CanWithdraw { get; } = true;
}
