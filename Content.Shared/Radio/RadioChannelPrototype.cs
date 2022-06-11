using Robust.Shared.Prototypes;

namespace Content.Shared.Radio
{
    [Prototype("radioChannel")]
    public sealed class RadioChannelPrototype : IPrototype
    {
        // Human-readable name for the channel.
        [ViewVariables] [DataField("name")] public string Name { get; private set; } = string.Empty;

        // Single-character prefix to determine what channel a message should be sent to.
        [ViewVariables] [DataField("keycode")] public char KeyCode { get; private set; } = '\0';

        // Integer frequency of this channel.
        [ViewVariables] [DataField("channel")] public int Channel { get; private set; } = 0;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
