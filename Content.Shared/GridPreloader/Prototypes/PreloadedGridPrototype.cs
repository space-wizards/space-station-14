using Robust.Shared.Prototypes;

namespace Content.Shared.GridPreloader.Prototypes;

[Prototype("preloadedGrid")]
public sealed partial class PreloadedGridPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField(required: true)]
    public string? Path;

    [DataField]
    public int Copies = 1;
}
