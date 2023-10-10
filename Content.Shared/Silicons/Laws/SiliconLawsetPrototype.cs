using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Silicons.Laws;

[Virtual, DataDefinition]
[Serializable, NetSerializable]
public partial class SiliconLawset
{
    [DataField("laws", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<SiliconLawPrototype>))]
    public List<string> Laws = new();
}

/// <summary>
/// This is a prototype for a lawset governing the behavior of silicons.
/// </summary>
[Prototype("siliconLawset")]
[Serializable, NetSerializable]
public sealed class SiliconLawsetPrototype : SiliconLawset, IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;


}
