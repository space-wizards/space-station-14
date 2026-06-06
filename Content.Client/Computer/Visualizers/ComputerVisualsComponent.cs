namespace Content.Client.Computer.Visualizers;

/// <summary>
///
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
    public string? StateKeys;

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
}
