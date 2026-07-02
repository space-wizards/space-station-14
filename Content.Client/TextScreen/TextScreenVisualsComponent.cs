using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

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
    [DataField]
    public Color Color = new Color(15, 151, 251);

    /// <summary>
    ///     Offset for centering the text.
    /// </summary>
    [DataField]
    public Vector2 TextOffset = Vector2.Zero;

    /// <summary>
    ///     Offset for centering the timer.
    /// </summary>
    [DataField]
    public Vector2 TimerOffset = Vector2.Zero;

    /// <summary>
    ///     Number of rows of text this screen can render.
    /// </summary>
    [DataField]
    public int Rows = 2;

    /// <summary>
    ///     Vertical distance between the top pixel of each row.
    /// </summary>
    [DataField]
    public int RowOffset = 7;

    /// <summary>
    ///     The amount of characters this component can show per row.
    /// </summary>
    /// <remarks>
    ///     Note that scrolling text can show one more than this.
    /// </remarks>
    [DataField]
    public int RowLength = 5;

    /// <summary>
    ///     Text the screen should show when it finishes a timer.
    /// </summary>
    [DataField]
    public string?[] Text = new string?[2];

    /// <summary>
    ///     Text the screen will draw whenever appearance is updated.
    /// </summary>
    public string?[] TextToDraw = new string?[2];

    /// <summary>
    ///     Per-character layers, for mapping into the sprite component.
    /// </summary>
    [DataField]
    public Dictionary<string, string?> LayerStatesToDraw = new();

    /// <summary>
    ///     If true, the screen is able to scroll its text.
    ///     Not used for timers.
    /// </summary>
    [DataField]
    public bool ScrollEnabled;

    /// <summary>
    ///     The next time that the text on each row should be scrolled.
    /// </summary>

    [DataField(customTypeSerializer: typeof(CustomArraySerializer<TimeSpan, TimeOffsetSerializer>))]
    public TimeSpan[] NextScrollTime = [TimeSpan.MaxValue, TimeSpan.MaxValue];

    /// <summary>
    ///     The amount of time between scrolling individual pixels per row.
    /// </summary>

    [DataField]
    public TimeSpan[] TimeBetweenScrolls = [TimeSpan.MaxValue, TimeSpan.MaxValue];

    /// <summary>
    ///     A counter the scroll position of each row.
    ///     Should be used modulo the pixel width of the actual strings.
    /// </summary>
    [DataField]
    public int[] ScrollPosition = new int[2];

    /// <summary>
    ///     The last received text for this screen. Prevents resetting the scroll state on updates.
    /// </summary>
    [DataField]
    public string? LastText;

    /// <summary>
    ///     The layer for the outer frame of the text screen.
    ///     Will be registered on top of the other layers.
    /// </summary>
    [DataField]
    public PrototypeLayerData? FrameState;

    [DataField]
    public string HourFormat = "D2";
    [DataField]
    public string MinuteFormat = "D2";
    [DataField]
    public string SecondFormat = "D2";
}
