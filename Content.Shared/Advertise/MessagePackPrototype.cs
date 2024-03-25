using Robust.Shared.Prototypes;

namespace Content.Shared.Advertise;

[Serializable, Prototype("messagePack")]
public sealed partial class MessagePackPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<LocId> Messages { get; private set; } = [];
}
