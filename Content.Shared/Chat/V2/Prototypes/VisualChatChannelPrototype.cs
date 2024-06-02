using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.V2.Prototypes;

[Prototype("visualChatChannel")]
public sealed partial class VisualChatChannelPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;
}
