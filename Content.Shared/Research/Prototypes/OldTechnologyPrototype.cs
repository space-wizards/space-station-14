using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Prototypes;

//todo delete
[NetSerializable, Serializable, Prototype("oldTechnology")]
public sealed class OldTechnologyPrototype : IPrototype
{
    /// <summary>
    ///     The ID of this technology prototype.
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;
}

