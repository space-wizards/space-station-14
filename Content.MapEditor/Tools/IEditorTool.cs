using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Interface for editor tools that respond to mouse input over the map viewport.
/// </summary>
public interface IEditorTool
{
    string Name { get; }
    void OnMouseDown(ToolContext ctx, Vector2i tilePos, EntityUid gridUid);
    void OnMouseDrag(ToolContext ctx, Vector2i tilePos, EntityUid gridUid);
    void OnMouseUp(ToolContext ctx);
}
