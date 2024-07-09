using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.V2.Prototypes;

[Prototype("outOfCharacterChannel")]
public sealed partial class OutOfCharacterChannelPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;

    [DataField("observersOnly")]
    public bool ObserversOnly;

    [DataField("roundEndOnly")]
    public bool RoundEndOnly;

    [DataField("adminsReadOnly")]
    public bool AdminsReadOnly;

    [DataField("adminsWriteOnly")]
    public bool AdminsWriteOnly;
}
