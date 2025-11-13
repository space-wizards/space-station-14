using Robust.Shared.Serialization;

namespace Content.Shared.DisplacementMap;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DisplacementData
{
    /// <summary>
    /// allows you to attach different maps for layers of different sizes.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<int, PrototypeLayerData> SizeMaps = new();

    [DataField]
    public string? ShaderOverride = "DisplacedDraw";
}
