using Content.Shared.Screen;
using System.Numerics;
using System.Collections.Generic;
using Robust.Client.Graphics;

namespace Content.Client.Screen;

[RegisterComponent]
public sealed partial class ScreenComponent : Component
{
    /// <summary>
    ///     1/32 - the size of a pixel
    /// </summary>
    public const float PixelSize = 1f / EyeManager.PixelsPerMeter;

    /// <summary>
    ///     Color used for every ScreenUpdate that doesn't supply one
    ///     15,151,251 is the old ss13 color, from tg
    /// </summary>
    [DataField("defaultColor"), ViewVariables(VVAccess.ReadWrite)]
    public Color DefaultColor = new Color(15, 151, 251);

    /// <summary>
    ///     Offset for centering the text.
    /// </summary>
    [DataField("textOffset"), ViewVariables(VVAccess.ReadWrite)]
    public Vector2 TextOffset = Vector2.Zero;

    /// <summary>
    ///    Offset for centering the timer.
    /// </summary>
    [DataField("timerOffset"), ViewVariables(VVAccess.ReadWrite)]
    public Vector2 TimerOffset = Vector2.Zero;

    /// <summary>
    ///     Number of rows of text this screen can render.
    /// </summary>
    [DataField("rows")]
    public int Rows = 2;

    /// <summary>
    ///     Spacing between each text row
    /// </summary>
    [DataField("rowOffset")]
    public int RowOffset = 7;

    /// <summary>
    ///     The amount of characters this component can show per row.
    /// </summary>
    [DataField("rowLength")]
    public int RowLength = 5;

    /// <summary>
    ///     Per-character layers, for mapping into the sprite component.
    /// </summary>
    [DataField("layerStatesToDraw")]
    public Dictionary<string, string?> LayerStatesToDraw = new();

    [DataField("hourFormat")]
    public string HourFormat = "D2";
    [DataField("minuteFormat")]
    public string MinuteFormat = "D2";
    [DataField("secondFormat")]
    public string SecondFormat = "D2";

    [ViewVariables(VVAccess.ReadWrite)]
    public ScreenUpdate? ActiveUpdate;
    [ViewVariables(VVAccess.ReadWrite)]
    public SortedDictionary<ScreenPriority, ScreenUpdate> Updates = new();
}
