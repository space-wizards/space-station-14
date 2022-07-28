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

        /// <summary>
        /// Should this radiochannel skip telecomms entirely (no logging or anything)? Used for private channels like syndie or cc
        /// </summary>
        [ViewVariables] [DataField("skipTelecomms")] public bool SkipTelecomms { get; private set; } = false;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
