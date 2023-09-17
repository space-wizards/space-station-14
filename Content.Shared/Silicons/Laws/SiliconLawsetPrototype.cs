using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// Lawset data used internally.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class SiliconLawset
{
    /// <summary>
    /// List of laws in this lawset.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public List<SiliconLaw> Laws = new();
}

/// <summary>
/// This is a prototype for a <see cref="SiliconLawPrototype"/> list.
/// Cannot be used directly since it is a list of prototype ids rather than List<Siliconlaw>.
/// </summary>
[Prototype("siliconLawset"), Serializable, NetSerializable]
public sealed partial class SiliconLawsetPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// List of law prototype ids in this lawset.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<SiliconLawPrototype>))]
    public List<string> Laws = new();
}
