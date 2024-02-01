using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Storage;

/// <summary>
/// Defines a preset to govern the behavior of StorageOverrideSystem.
/// The prototype carries the preset, rules and behavior.
/// The component references what preset the entity reacts to.
/// The component decides what other items the entity may be replaced with.
/// </summary>
[Prototype("storageOverride")]
public sealed partial class StorageOverridePrototype : IPrototype
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public string Preset { get; private set; } = string.Empty;

    [DataField]
    private int _maxReplacements = int.MaxValue;

    public int MaxReplacements {
        get => _maxReplacements;
        private set => _maxReplacements = Math.Max(1, value);
    }

    [DataField]
    public bool SearchReplaced { get; private set; } = true;

    [DataField]
    public bool AllowDrop { get; private set; } = true;

    /// If you change the following fields, do the same in StorageOverrideComponent
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
