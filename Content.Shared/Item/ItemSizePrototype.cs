using Robust.Shared.Prototypes;

namespace Content.Shared.Item;

/// <summary>
/// This is a prototype for a category of an item's size.
/// </summary>
[Prototype("itemSize")]
public sealed partial class ItemSizePrototype : IPrototype, IComparable<ItemSizePrototype>
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The amount of space in a bag an item of this size takes.
    /// </summary>
    [DataField]
    public int Weight = 1;

    /// <summary>
    /// A player-facing name used to describe this size.
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// The default inventory shape associated with this item size.
    /// </summary>
    [DataField(required: true)]
    public IReadOnlyList<Box2i> DefaultShape = new List<Box2i>();

    public int CompareTo(ItemSizePrototype? other)
    {
        if (other is not { } otherItemSize)
            return 0;
        return Weight.CompareTo(otherItemSize.Weight);
    }

    public static bool operator <(ItemSizePrototype a, ItemSizePrototype b)
    {
        return a.Weight < b.Weight;
    }

    public static bool operator >(ItemSizePrototype a, ItemSizePrototype b)
    {
        return a.Weight > b.Weight;
    }

    public static bool operator <=(ItemSizePrototype a, ItemSizePrototype b)
    {
        return a.Weight <= b.Weight;
    }

    public static bool operator >=(ItemSizePrototype a, ItemSizePrototype b)
    {
        return a.Weight >= b.Weight;
    }
}
