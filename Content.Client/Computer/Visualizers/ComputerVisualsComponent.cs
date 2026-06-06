namespace Content.Client.Computer.Visualizers;

/// <summary>
/// A component to set up visuals for computers.
/// Sets up initial states (RSI expected to be correct).
/// Hides and shows screen, handles key shading with power updates.
/// </summary>
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
    /// The RSI state used for the screen of the maintenance panel.
    /// </summary>
    [DataField]
    public string? StatePanel;
}
