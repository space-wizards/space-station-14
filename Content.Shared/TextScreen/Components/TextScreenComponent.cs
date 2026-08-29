using System.Numerics;
using Content.Shared.TextScreen.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// A component for rendering text on a screen.
/// Can show scrolling text, timers, or other specific-use information (e.g. arrivals timer)
/// </summary>
/// <remarks>
/// Pausing handled manually due to non-trivial TextScreenRow logic.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(TextScreenSystem))]
[AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class TextScreenComponent : Component
{
    /// <summary>
    /// 1/32 - the size of a pixel in meters.
    /// NOTE: the magical EyeManager size isn't available
    /// </summary>
    public const float PixelSize = 1f / 32f;

    /// <summary>
    /// The color of the text drawn.
    /// </summary>
    /// <remarks>
    /// 15,151,251 is the old ss13 color, from tg
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Color Color = new Color(15, 151, 251);

    /// <summary>
    /// The last received color, useful on the client.
    /// </summary>
    [DataField]
    public Color LastColor;

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
    /// If true, the screen is able to scroll its text.
    /// </summary>
    [DataField]
    public bool ScrollEnabled;

    /// <summary>
    /// The list of row data for the text screens.
    /// Only useful client-side.
    /// </summary>
    [DataField]
    public TextScreenRow[] RowData = new TextScreenRow[2];

    /// <summary>
    /// The text to display on the screen.
    /// Each row delimited with a newline (\n) character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Text;

    /// <summary>
    /// The time that the text was sent.
    /// Used for scrolling.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan TextTime;

    /// <summary>
    /// The last value of <see cref="Text"/> received from the server.
    /// Only used client-side!
    /// </summary>
    [DataField]
    public string? TextToDisplay;

    /// <summary>
    /// The last time TextToDisplay was updated.
    /// Only used client-side!
    /// </summary>
    [DataField]
    public TimeSpan? DisplayTime;

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
[DataRecord]
[Serializable, NetSerializable]
public partial struct TextScreenRow()
{
    /// <summary>
    /// The time this row should next scroll a pixel.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextScroll;

    /// <summary>
    /// The amount of time this screen should spend scrolling.
    /// </summary>
    public TimeSpan ScrollDelay;

    /// <summary>
    /// The current position of the row in the string, in pixels.
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
