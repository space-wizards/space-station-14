using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Interface for editor tools that respond to mouse input over the map viewport.
///     Tools read the active grid from <see cref="ToolContext.ActiveGridUid"/>.
/// </summary>
public interface IEditorTool
{
    string Name { get; }
    void OnMouseDown(ToolContext ctx, Vector2i tilePos);
    void OnMouseDrag(ToolContext ctx, Vector2i tilePos);
    void OnMouseUp(ToolContext ctx);
}
