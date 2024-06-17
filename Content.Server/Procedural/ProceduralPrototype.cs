using Robust.Shared.Prototypes;

namespace Content.Server.Procedural;

/// <summary>
/// Template prototype that can be used for <see cref="ProceduralComponent"/> defaults.
/// </summary>
[Prototype]
public sealed partial class ProceduralPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField]
    public List<ProceduralMetaLayer> Layers = new();
}

[Prototype]
public sealed partial class ProceduralPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;
}

/// <summary>
/// When added to a map will continuously generate procedural data.
/// </summary>
[RegisterComponent]
public sealed partial class ProceduralComponent : Component
{
    public ProtoId<ProceduralPrototype>? TemplateProto;

    [DataField]
    public List<ProceduralMetaLayer> Layers = new();
}

[DataRecord]
public record struct ProceduralMetaLayer()
{
    /// <summary>
    /// How large chunks are in this meta-layer.
    /// </summary>
    public Vector2i ChunkSize = new Vector2i(8, 8);
}
