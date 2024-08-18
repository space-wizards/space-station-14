using System.Numerics;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Tndicates that this entity can be inserted into FoodSequence, which will transfer all reagents to the target.
/// </summary>
[RegisterComponent, Access(typeof(SharedFoodSequenceSystem))]
public sealed partial class FoodSequenceElementComponent : Component
{
    /// <summary>
    /// the same object can be used in different sequences, and it will have a different sprite in different sequences.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, FoodSequenceElementEntry> Entries = new();

    /// <summary>
    /// which solution we will add to the main dish
    /// </summary>
    [DataField]
    public string Solution = "food";

    /// <summary>
    /// state used to generate the appearance of the added layer
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;
}

[DataRecord, Serializable, NetSerializable]
public sealed class FoodSequenceElementEntry
{
    /// <summary>
    /// A localized name piece to build into the item name generator.
    /// </summary>
    public LocId? Name { get; set; } = null;

    /// <summary>
    /// overriding default sprite
    /// </summary>
    public SpriteSpecifier? Sprite { get; set; } = null;

    /// <summary>
    /// If the layer is the final one, it can be added over the limit, but no other layers can be added after it.
    /// </summary>
    public bool Final { get; set; } = false;

    /// <summary>
    /// the shear of a particular layer. Allows a little "randomization" of each layer.
    /// </summary>
    public Vector2 LocalOffset { get; set; } = Vector2.Zero;
}
