namespace Content.Shared.Lathe.Components;

/// <summary>
/// This is used for storing visual data for a <see cref="LatheComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class LatheVisualsComponent : Component
{
    /// <summary>
    /// Unshaded sprite state when this lathe is idle
    /// </summary>
    [DataField]
    public string? IdleState;

    /// <summary>
    /// Unshaded sprite state when this lathe is running
    /// </summary>
    [DataField]
    public string? RunningState;

    /// <summary>
    /// Shaded sprite state when this lathe is idle
    /// </summary>
    [DataField]
    public string? UnlitIdleState;

    /// <summary>
    /// Shaded sprite state when this lathe is running
    /// </summary>
    [DataField]
    public string? UnlitRunningState;
}
