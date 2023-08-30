using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components;

/// <summary>
///     Change sprite depending on a storage fill percent.
/// </summary>
[RegisterComponent]
public sealed partial class StorageFillVisualizerComponent : Component
{
    [DataField("maxFillLevels", required: true)]
    public int MaxFillLevels;

    [DataField("fillBaseName", required: true)]
    public string FillBaseName = default!;
}

[Serializable, NetSerializable]
public enum StorageFillVisuals : byte
{
    FillLevel
}

[Serializable, NetSerializable]
public enum StorageFillLayers : byte
{
    Fill
}
