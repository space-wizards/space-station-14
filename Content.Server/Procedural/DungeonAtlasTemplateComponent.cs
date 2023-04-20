using Robust.Shared.Utility;

namespace Content.Server.Procedural;

/// <summary>
/// Added to pre-loaded maps for dungeon templates.
/// </summary>
[RegisterComponent]
public sealed class DungeonAtlasTemplateComponent : Component
{
    [DataField("path", required: true)]
    public ResourcePath? Path;
}
