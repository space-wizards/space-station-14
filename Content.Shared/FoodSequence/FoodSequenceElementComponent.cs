namespace Content.Shared.FoodSequence;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(SharedFoodSequenceSystem))]
public sealed partial class FoodSequenceElementComponent : Component
{
    [DataField]
    public Dictionary<string, FoodSequenceElementEntry> Entries = new();
}

[DataRecord]
public partial record struct FoodSequenceElementEntry()
{
    public string Name { get; set; } = string.Empty;

    public string? State { get; set; } = null;
}
