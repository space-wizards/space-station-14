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

        [ViewVariables] [DataField("frequency")] public int Frequency { get; private set; } = 0;

        [ViewVariables] [DataField("color")] public Color Color { get; private set; } = Color.Lime;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
