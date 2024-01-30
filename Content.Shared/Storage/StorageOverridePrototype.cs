using Robust.Shared.Prototypes;

namespace Content.Shared.Storage;

[Prototype("storageOverride")]
public sealed partial class StorageOverridePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public int MaxReplacements { get; private set; } = int.MaxValue;

    [DataField]
    public bool SearchReplaced { get; private set; } = true;

    [DataField]
    public string? Species { get; private set; } = null;

    [DataField]
    public string? SlotName { get; private set; } = null;

    [DataField]
    public Dictionary<string, string> Prototypes = new();
}
