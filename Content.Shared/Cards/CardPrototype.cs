using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Cards;

/// <summary>
/// Defines the visuals for a card.
/// </summary>
[Prototype]
public sealed partial class CardPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CardPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The RSI path used for this card's layers.
    /// </summary>
    [DataField(required: true)]
    public string Sprite;

    /// <summary>
    /// Colour applied to any layer that doesn't specify its own colour override.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// Layers making up the card's face, drawn in list order (later entries render on top).
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<CardLayerData> Layers = new();

    /// <summary>
    /// The sprite for the background of the card. Will override any base state set by a deck of cards.
    /// <summary>
    [DataField]
    public string? BaseState;

    /// <summary>
    /// The sprite state for the back of the card. Will override any back sprite set by a deck of cards.
    /// </summary>
    [DataField]
    public string? CardBack;
}

/// <summary>
/// A single visual layer on a card.
/// </summary>
[DataRecord]
public sealed partial class CardLayerData
{
    /// <summary>
    /// The sprite state for this layer.
    /// </summary>
    [DataField(required: true)]
    public string State = default!;

    /// <summary>
    /// This layer's colour. If unset, falls back to the owning prototype's <see cref="CardPrototype.Color"/>.
    /// If both unset, no colour change will be applied.
    /// </summary>
    public Color? Color;
}
