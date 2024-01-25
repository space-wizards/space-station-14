using Robust.Shared.Prototypes;

namespace Content.Shared.Shuttles.Prototypes;

[Prototype("preloadedShuttle")]
public sealed partial class PreloadedShuttlePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField(required: true)]
    public string? Path;

    [DataField]
    public int Copies = 1;
}
