using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.Prototypes;

[Prototype("communicationType")]
public sealed partial class CommunicationTypePrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;
}
