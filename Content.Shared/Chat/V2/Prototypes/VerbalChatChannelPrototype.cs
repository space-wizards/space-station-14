using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.V2.Prototypes;

[Prototype("verbalChatChannel")]
public sealed partial class VerbalChatChannelPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;

    [DataField("range")]
    public float Range = 10.0f;

    [DataField("hideInChatLogs")]
    public bool HideInChatLogs;
}
