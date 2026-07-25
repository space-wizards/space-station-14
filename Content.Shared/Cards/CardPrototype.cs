using Robust.Shared.Prototypes;

namespace Content.Shared.Cards;

[Prototype]
public sealed partial class CardPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    // First layer sprite, will be below layer two but above base layer
    [DataField]
    public string? LayerOneState { get; private set; }

    // First layer colour. If left unused sprite will have no colour change applied
    [DataField]
    public Color? LayerOneColor { get; private set; }

    // Second layer sprite, will be highest layer
    [DataField]
    public string? LayerTwoState { get; private set; }

    // Second layer colour. If left unused sprite will have no colour change applied
    [DataField]
    public Color? LayerTwoColor { get; private set; }

    // The sprite for the background of the card. Will override any base state set by a deck of cards.
    [DataField]
    public string? BaseState { get; private set; }

    // The sprite for the back of the card. Will override any back sprite set by a deck of cards.
    [DataField]
    public string? CardBack { get; private set; }
}
