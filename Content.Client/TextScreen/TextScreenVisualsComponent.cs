using System.Numerics;
using Content.Shared.TextScreen;
using Robust.Client.Graphics;

namespace Content.Client.TextScreen;

[RegisterComponent]
public sealed partial class TextScreenVisualsComponent : Component
{
    /// <summary>
    ///     1/32 - the size of a pixel
    /// </summary>
    public const float PixelSize = 1f / EyeManager.PixelsPerMeter;

    /// <summary>
    ///     The color of the text drawn.
    /// </summary>
    [DataField("color")]
    public Color Color { get; set; } = Color.FloralWhite;

    /// <summary>
    ///     Whether the screen is on.
    /// </summary>
    [DataField("activated")]
    public bool Activated;

    /// <summary>
    ///     Offset for drawing the text. <br/>
    ///     (0, 8) pixels is the default for the Structures\Wallmounts\textscreen.rsi
    /// </summary>
    [DataField("textOffset"), ViewVariables(VVAccess.ReadWrite)]
    public Vector2 TextOffset { get; set; } = Vector2.Zero;

    [DataField("timerOffset"), ViewVariables(VVAccess.ReadWrite)]
    public Vector2 TimerOffset { get; set; } = Vector2.Zero;

    /// <summary>
    ///     Number of rows of text to render.
    /// </summary>
    [DataField("rows")]
    public int Rows { get; set; } = 1;

    /// <summary>
    ///     Spacing between each text row
    /// </summary>
    [DataField("rowOffset")]
    public int RowOffset { get; set; } = 0;

    /// <summary>
    ///     The amount of characters this component can show per row.
    /// </summary>
    [DataField("rowLength")]
    public int RowLength { get; set; } = 5;

    /// <summary>
    ///     Text the screen should show when it's not counting.
    /// </summary>
    [DataField("text"), ViewVariables(VVAccess.ReadWrite)]
    public string?[] Text = new string?[2];

    public string?[] TextToDraw = new string?[2];

    /// <summary>
    ///     The different layers for each character - this is the currently drawn states.
    /// </summary>
    [DataField("layerStatesToDraw")]
    public Dictionary<string, string?> LayerStatesToDraw = new();

    [DataField("hourFormat")]
    public string HourFormat { get; set; } = "D2";
    [DataField("minuteFormat")]
    public string MinuteFormat { get; set; } = "D2";
    [DataField("secondFormat")]
    public string SecondFormat { get; set; } = "D2";
}
