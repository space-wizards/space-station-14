using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("worldgenConfig")]
public sealed class WorldgenConfigPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("configData", required: true)]
    public List<object> ConfigData { get; } = default!;
}
