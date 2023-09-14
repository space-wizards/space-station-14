using System.Numerics;
using Content.Shared.TextScreen;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

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
    ///     The current mode of the screen - is it showing text, or currently counting?
    /// </summary>
    [DataField("currentMode")]
    public TextScreenMode CurrentMode = TextScreenMode.Text;

    /// <summary>
    ///     The time it is counting to or from.
    /// </summary>
    [DataField("targetTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TargetTime = TimeSpan.Zero;

    /// <summary>
    ///     Offset for drawing the text. <br/>
    ///     (0, 8) pixels is the default for the Structures\Wallmounts\textscreen.rsi
    /// </summary>
    [DataField("textOffset"), ViewVariables(VVAccess.ReadWrite)]
    public Vector2 TextOffset = new(0f, 8f * PixelSize);

    /// <summary>
    ///     The amount of characters this component can show.
    /// </summary>
    [DataField("textLength")]
    public int TextLength = 5;

    /// <summary>
    ///     Text the screen should show when it's not counting.
    /// </summary>
    [DataField("text"), ViewVariables(VVAccess.ReadWrite)]
    public string Text = "";

    public string TextToDraw = "";

    /// <summary>
    ///     The different layers for each character - this is the currently drawn states.
    /// </summary>
    [DataField("layerStatesToDraw")]
    public Dictionary<string, string?> LayerStatesToDraw = new();
}

