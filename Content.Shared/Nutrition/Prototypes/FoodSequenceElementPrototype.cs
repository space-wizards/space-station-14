using Content.Shared.Nutrition.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared.Nutrition.Prototypes;

/// <summary>
/// Unique data storage block for different FoodSequence layers
/// </summary>
[Prototype]
public sealed partial class FoodSequenceElementPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// sprite options. A random one will be selected and used to display the layer.
    /// </summary>
    [DataField]
    public List<SpriteSpecifier> Sprites { get; private set; } = new();

    /// <summary>
    /// Relative size of the sprite displayed in the food sequence.
    /// </summary>
    [DataField]
    public Vector2 Scale { get; private set; } = Vector2.One;

    /// <summary>
    ///     A base sprite offset that is applied to this element.
    /// </summary>
    [DataField]
    public Vector2 Offset { get; private set; } = Vector2.Zero;

    /// <summary>
    /// A localized name piece to build into the item name generator.
    /// </summary>
    [DataField]
    public LocId? Name { get; private set; }

    /// <summary>
    /// If the layer is the final one, it can be added over the limit, but no other layers can be added after it.
    /// </summary>
    [DataField]
    public bool Final { get; private set; }

    /// <summary>
    /// Tag list of this layer. Used for recipes for food metamorphosis.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> Tags { get; set; } = new();

    /// <summary>
    ///     If this is enabled, then this element will calculate a random offset when stacked
    ///     based on <see cref="FoodSequenceStartPointComponent.MinLayerOffset"/> and
    ///     <see cref="FoodSequenceStartPointComponent.MaxLayerOffset"/>.
    /// </summary>
    [DataField]
    public bool UseRandomOffset = true;

    /// <summary>
    ///     Whether or not this ingredient can be flipped when applied to a sequence that
    ///     has <see cref="FoodSequenceStartPointComponent.AllowHorizontalFlip"/>.
    /// </summary>
    [DataField]
    public bool CanFlip = true;
}
