namespace Content.Client.Computer.Visualizers;

/// <summary>
/// A component to set up visuals for computers.
/// </summary>
/// <remarks>
/// Sets up initial states. Each configured state requires a mapped sprite layer with the appropriate RSI.
/// Hides and shows screen, handles key shading with power updates.
/// </remarks>
[RegisterComponent]
[Access(typeof(ComputerVisualizerSystem))]
public sealed partial class ComputerVisualsComponent : Component
{
    /// <summary>
    /// The RSI state used for the frame of the computer.
    /// </summary>
    [DataField]
    public string? StateFrame;

    /// <summary>
    /// The RSI state used for the keyboard of the computer.
    /// </summary>
    [DataField]
    public string? StateKeyboard;

    /// <summary>
    /// The RSI state used for the screen of the computer.
    /// </summary>
    [DataField]
    public string? StateScreen;

    /// <summary>
    /// The RSI state used for the keyboard of the computer.
    /// </summary>
    [DataField]
    public string? StateKeys;

    /// <summary>
    /// The RSI state used for the open maintenance panel.
    /// </summary>
    [DataField]
    public string? StatePanel;
}
