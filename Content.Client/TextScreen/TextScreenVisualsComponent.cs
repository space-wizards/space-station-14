using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client.TextScreen;

/// <summary>
/// A component for rendering text on a screen.
/// Can show scrolling text, timers, or other specific-use information (e.g. arrivals timer)
/// </summary>
/// <remarks>
/// Pausing handled manually due to manual TextScreenRow logic.
/// </remarks>
[RegisterComponent, Access(typeof(TextScreenSystem))]
public sealed partial class TextScreenVisualsComponent : Component
{
    /// <summary>
    /// 1/32 - the size of a pixel in meters.
    /// </summary>
    public const float PixelSize = 1f / EyeManager.PixelsPerMeter;

    /// <summary>
    /// The color of the text drawn.
    /// </summary>
    /// <remarks>
    /// 15,151,251 is the old ss13 color, from tg
    /// </remarks>
    [DataField]
    public Color Color = new Color(15, 151, 251);

    /// <summary>
    /// The current color being drawn on the screen.
    /// </summary>
    [ViewVariables]
    public Color CurrentColor;

    /// <summary>
    /// Offset for centering the text.
    /// </summary>
    [DataField]
    public Vector2 TextOffset = Vector2.Zero;

    /// <summary>
    /// Offset for centering the timer.
    /// </summary>
    [DataField]
    public Vector2 TimerOffset = Vector2.Zero;

    /// <summary>
    /// Vertical distance between the top pixel of each row.
    /// </summary>
    [DataField]
    public int RowOffset = 7;

    /// <summary>
    /// The amount of characters this component can show per row.
    /// </summary>
    /// <remarks>
    /// Note that scrolling text can show one more than this.
    /// </remarks>
    [DataField]
    public int RowLength = 5;

    /// <summary>
    /// When scrolling, a horizontal offset for the scrolling, in pixels
    /// </summary>
    /// <seealso cref="TextScreenSystem.CharWidth"/>
    [DataField]
    public int HorizontalScrollOffset;

    /// <summary>
    /// When scrolling, the number of pixels that the leftmost letters should be invisible for.
    /// Value should be between [0,CharWidth)
    /// </summary>
    /// <seealso cref="TextScreenSystem.CharWidth"/>
    [DataField]
    public int LeftInvisiblePixels;

    /// <summary>
    /// When scrolling, the number of pixels that the rightmost letters should be invisible for.
    /// Value should be between [0,CharWidth)
    /// </summary>
    /// <seealso cref="TextScreenSystem.CharWidth"/>
    [DataField]
    public int RightInvisiblePixels;

    /// <summary>
    /// If true, the screen is able to scroll its text.
    /// </summary>
    [DataField]
    public bool ScrollEnabled;

    /// <summary>
    /// The list of row data for the text screens.
    /// Only useful client-side.
    /// </summary>
    [DataField]
    public TextScreenRow[] RowData = { new(), new() };

    /// <summary>
    /// The text to display on the screen.
    /// Each row delimited with a newline (\n) character.
    /// </summary>
    [ViewVariables]
    public string? TextToDisplay;

    /// <summary>
    /// The time that the text was sent.
    /// Used for scrolling.
    /// </summary>
    [ViewVariables]
    public TimeSpan TextTime;

    /// <summary>
    /// If true, text to display has been updated and should redraw.
    /// </summary>
    [ViewVariables]
    public bool NewTextToDisplay;

    /// <summary>
    /// The layer for the outer frame of the text screen.
    /// Will be registered on top of the other layers.
    /// </summary>
    [DataField]
    public PrototypeLayerData? FrameState;
}

/// <summary>
/// All information about a given row of text.
/// </summary>
[DataRecord, Serializable]
public partial struct TextScreenRow()
{
    /// <summary>
    /// The time this row should next scroll the text by a pixel.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextScroll = TimeSpan.MaxValue;

    /// <summary>
    /// The delay between each pixel scrolled on this screen.
    /// </summary>
    public TimeSpan ScrollDelay = TimeSpan.MaxValue;

    /// <summary>
    /// The current position of the row in the string, in pixels.
    /// Increases monotonically, should be taken modulo the text length.
    /// Each character is a fixed size (assumed 4 pixels wide)
    /// </summary>
    public int ScrollPosition;

    /// <summary>
    /// A list with each of the row's sprite layers, with the key inside of it and the state it's currently on.
    /// </summary>
    public List<(string Key, string? State)> Layers = new();

    /// <summary>
    /// The full text currently being drawn on the row.
    /// </summary>
    public string Text = string.Empty;
}
