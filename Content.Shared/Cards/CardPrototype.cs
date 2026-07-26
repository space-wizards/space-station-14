using Robust.Shared.Prototypes;

namespace Content.Shared.Cards;

/// <summary>
/// Defines the visuals for a card.
/// </summary>
[Prototype]
public sealed partial class CardPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// First layer sprite, will be below layer two but above base layer
    /// <summary>
    [DataField]
    public string? LayerOneState { get; private set; }

    /// <summary>
    /// First layer colour. If left unused sprite will have no colour change applied
    /// <summary>
    [DataField]
    public Color? LayerOneColor { get; private set; }

    /// <summary>
    /// Second layer sprite, will be highest layer
    /// <summary>
    [DataField]
    public string? LayerTwoState { get; private set; }

    /// <summary>
    /// Second layer colour. If left unused sprite will have no colour change applied
    /// <summary>
    [DataField]
    public Color? LayerTwoColor { get; private set; }

    /// <summary>
    /// The sprite for the background of the card. Will override any base state set by a deck of cards.
    /// <summary>
    [DataField]
    public string? BaseState { get; private set; }

    /// <summary>
    /// The sprite for the back of the card. Will override any back sprite set by a deck of cards.
    /// <summary>
    [DataField]
    public string? CardBack { get; private set; }
}
