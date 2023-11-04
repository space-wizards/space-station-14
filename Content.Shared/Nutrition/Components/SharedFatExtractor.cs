using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components;

[Serializable, NetSerializable]
public enum FatExtractorVisuals : byte
{
    Processing
}

public enum FatExtractorVisualLayers : byte
{
    Light,
    Stack,
    Smoke
}
