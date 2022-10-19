using Robust.Shared.Prototypes;

namespace Content.Shared.Radio
{
    [Prototype("radioChannel")]
    public readonly record struct RadioChannelPrototype : IPrototype
    {
        /// <summary>
        /// Human-readable name for the channel.
        /// </summary>
        [ViewVariables]
        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)] public string LocalizedName => Loc.GetString(Name);

        /// <summary>
        /// Single-character prefix to determine what channel a message should be sent to.
        /// </summary>
        [ViewVariables]
        [DataField("keycode")]
        public char KeyCode { get; } = '\0';

        [ViewVariables]
        [DataField("frequency")]
        public int Frequency { get; }

        [ViewVariables] [DataField("color")] public Color Color { get; } = Color.Lime;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
