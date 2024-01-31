using Robust.Shared.Random;

namespace Content.Shared.Storage;

/// <summary>
/// Marks an item as overridable using information from a StorageOverridePrototype preset.
/// The prototype carries the preset, rules and behavior.
/// The component references what preset the entity reacts to.
/// The component decides what other items the entity may be replaced with.
/// </summary>
[RegisterComponent]
public sealed partial class SharedStorageOverrideComponent : Component
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField(required: true)]
    public string Preset { get; private set; } = string.Empty;

    [DataField]
    private string? Single { get; set; } = null;

    [DataField]
    private List<string>? Random { get; set; } = null;

    [DataField]
    private Dictionary<string, string>? Keyed { get; set; } = null;

    public string? Pick(string? key = null)
    {
        if (Single != null)
            return Single;
        else if (Random != null)
            return Random[_random.Next(Random.Count)];
        else if (Keyed != null && key != null)
            return Keyed.GetValueOrDefault(key);
        else
            return null;
    }
}
