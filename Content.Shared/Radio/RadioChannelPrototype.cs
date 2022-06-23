using Robust.Shared.Prototypes;

namespace Content.Shared.Radio
{
    [Prototype("radioChannel")]
    public sealed class RadioChannelPrototype : IPrototype
    {
        /// <summary>
        /// Human-readable name for the channel.
        /// </summary>
        [ViewVariables] [DataField("name")] public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Single-character prefix to determine what channel a message should be sent to.
        /// </summary>
        [ViewVariables] [DataField("keycode")] public char KeyCode { get; private set; } = '\0';

        // Integer frequency of this channel.
        [ViewVariables] [DataField("channel")] public int Channel { get; private set; } = 0;

        [ViewVariables] [DataField("color")] public Color Color { get; private set; } = Color.White;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
