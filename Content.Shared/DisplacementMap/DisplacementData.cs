namespace Content.Shared.DisplacementMap;

[DataDefinition]
public sealed partial class DisplacementData
{
    [DataField(required: true)]
    public PrototypeLayerData Layer = default!;

    [DataField]
    public string? ShaderOverride = "DisplacedStencilDraw";
}
