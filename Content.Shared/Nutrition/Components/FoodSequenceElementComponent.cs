using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Serialization;

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
    [DataField]
    public Dictionary<string, FoodSequenceElementEntry> Entries = new();
}

[DataRecord, Serializable, NetSerializable]
public partial record struct FoodSequenceElementEntry()
{
    /// <summary>
    /// A localized name piece to build into the item name generator.
    /// </summary>
    public LocId? Name { get; set; } = null;

    /// <summary>
    /// state used to generate the appearance of the added layer
    /// </summary>
    public string? State { get; set; } = null;

    /// <summary>
    /// If the layer is the final one, it can be added over the limit, but no other layers can be added after it.
    /// </summary>
    public bool Final { get; set; } = false;
}

[Serializable, NetSerializable]
public enum FoodSequenceVisuals : byte
{
    Layers
}
