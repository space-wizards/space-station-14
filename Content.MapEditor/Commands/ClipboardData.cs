using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.MapEditor.Commands;

/// <summary>
///     Holds copied tile data for paste operations.
///     Tile positions are stored relative to (0,0) offset from the selection's min corner.
/// </summary>
public sealed class ClipboardData
{
    /// <summary>
    ///     Tiles stored relative to (0,0). Key is offset from the selection's min corner.
    /// </summary>
    public Dictionary<Vector2i, Tile> Tiles { get; } = new();

    /// <summary>
    ///     The size of the copied region (width, height).
    /// </summary>
    public Vector2i Size { get; set; }
}
