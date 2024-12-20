using Robust.Shared.Map;

namespace Content.Server.ShadowDimension;

[RegisterComponent]
public sealed partial class StationShadowDimensionComponent : Component
{
    [DataField]
    public MapId? DimensionId;
}
