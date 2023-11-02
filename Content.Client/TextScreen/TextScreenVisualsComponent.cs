using System.Numerics;
using Content.Shared.TextScreen;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client.TextScreen;

[RegisterComponent]
public sealed partial class TextScreenVisualsComponent : Component
{
    /// <summary>
    ///     Number of rows of text to render.
    /// </summary>
    [DataField("rows")]
    public int Rows = 1;

    /// <summary>
    ///     Spacing between each text row
    /// </summary>
    [DataField("rowOffset")]
    public Vector2 RowOffset = Vector2.Zero;
}

