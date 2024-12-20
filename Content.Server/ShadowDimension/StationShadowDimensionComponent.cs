using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.ShadowDimension;

[RegisterComponent]
public sealed partial class StationShadowDimensionComponent : Component
{
    [DataField]
    public MapId? DimensionId;

    [DataField]
    public Dictionary<ProtoId<TagPrototype>, EntProtoId> Replacements = new();
}
