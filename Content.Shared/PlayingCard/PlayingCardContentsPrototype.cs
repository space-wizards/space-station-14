using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.PlayingCard
{
    [Serializable, NetSerializable, Prototype("PlayingCardContents")]
    public sealed class PlayingCardContentsPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("cardPrototype")]
        public string CardPrototype { get; } = string.Empty;
        [DataField("cardHandPrototype")]
        public string CardHandPrototype { get; } = string.Empty;

        [DataField("baseLayerState")]
        public string BaseLayerState { get; } = string.Empty;

        [DataField("cardContents")]
        public Dictionary<string, string> CardContents { get; } = new();
    }

    [Serializable, NetSerializable, Prototype("PlayingCardDetails")]
    public sealed class PlayingCardDetailsPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;
        [DataField("layerOneState")]

        public string? LayerOneState { get; }
        [DataField("layerOneColor")]

        public Color? LayerOneColor { get; }
        [DataField("layerTwoState")]

        public string? LayerTwoState { get; }
        [DataField("layerTwoColor")]
        public Color? LayerTwoColor { get; }
    }
}
