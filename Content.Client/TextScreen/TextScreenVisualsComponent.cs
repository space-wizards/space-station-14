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
    /// <remarks>
    ///     15,151,251 is the old ss13 color, from tg
    /// </remarks>
    [DataField("color"), ViewVariables(VVAccess.ReadWrite)]
    public Color Color = new Color(15, 151, 251);

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
    public int Rows = 1;

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
    ///     Text the screen should show when it finishes a timer.
    /// </summary>
    [DataField("text"), ViewVariables(VVAccess.ReadWrite)]
    public string?[] Text = new string?[2];

    /// <summary>
    ///     Text the screen will draw whenever appearance is updated.
    /// </summary>
    public string?[] TextToDraw = new string?[2];

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
}
