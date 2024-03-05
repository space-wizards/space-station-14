using Robust.Shared.Prototypes;

namespace Content.Server.Advertise;

[Serializable, Prototype("messagePack"), Virtual]
public partial class MessagePackPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<string> Messages { get; private set; } = [];
}
