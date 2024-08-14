using System.Numerics;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Indicates that this entity can be inserted into FoodSequence, which will transfer all reagents to the target.
/// </summary>
[RegisterComponent, Access(typeof(SharedFoodSequenceSystem))]
public sealed partial class FoodSequenceElementComponent : Component
{
    /// <summary>
    /// Standard data that can be overwritten for individual keys in Entries
    /// </summary>
    [DataField]
    public FoodSequenceElementEntry Data = new();

    /// <summary>
    /// The same object can be used in different sequences, and it will have a different data in then.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, FoodSequenceElementEntry> Entries = new();

    /// <summary>
    /// Which solution we will add to the main dish
    /// </summary>
    [DataField]
    public string Solution = "food";
}

[DataRecord, Serializable, NetSerializable]
public sealed class FoodSequenceElementEntry
{
    /// <summary>
    /// A localized name piece to build into the item name generator.
    /// </summary>
    public LocId? Name { get; set; }

    /// <summary>
    /// Sprite rendered in sequence
    /// </summary>
    public SpriteSpecifier? Sprite { get; set; }

    /// <summary>
    /// Relative size of the sprite displayed in FoodSequence
    /// </summary>
    public Vector2 Scale  { get; set; } = Vector2.One;

    /// <summary>
    /// If the layer is the final one, it can be added over the limit, but no other layers can be added after it.
    /// </summary>
    public bool Final { get; set; }

    /// <summary>
    /// The offset of a particular layer. Allows a little position randomization of each layer.
    /// </summary>
    public Vector2 LocalOffset { get; set; } = Vector2.Zero;

    public List<ProtoId<TagPrototype>> Tags { get; set; }  = new();

    public static FoodSequenceElementEntry Clone(FoodSequenceElementEntry original)
    {
        FoodSequenceElementEntry clone = new()
        {
            Name = original.Name,
            Sprite = original.Sprite,
            Scale = original.Scale,
            Final = original.Final,
            Tags = original.Tags
        };

        return clone;
    }
}
