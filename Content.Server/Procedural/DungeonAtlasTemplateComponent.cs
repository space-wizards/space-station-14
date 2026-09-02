using Content.Shared.Maps;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

/// <summary>
/// Added to pre-loaded maps for dungeon templates.
/// </summary>
[RegisterComponent]
public sealed partial class DungeonAtlasTemplateComponent : Component
{
    [DataField("path", required: true, customTypeSerializer: typeof(MapResPathSerializer))]
    public ResPath Path;
}
