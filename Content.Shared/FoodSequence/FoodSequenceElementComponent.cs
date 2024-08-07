using Robust.Shared.Serialization;

namespace Content.Shared.FoodSequence;

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
    public LocId? Name { get; set; } = null;

    public string? State { get; set; } = null;
}
