namespace Content.Shared.CartridgeLoader.Cartridges;

/// <summary>
///     Component that indicates a PDA cartridge as containing the NanoTask program
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class NanoTaskCartridgeComponent : Component
{
    /// <summary>
    /// The list of tasks
    /// </summary>
    [DataField]
    public List<NanoTaskItemAndId> StationTasks = [];

    [DataField]
    public Dictionary<string, List<NanoTaskItemAndId>> DepartmentTasks = [];

    /// <summary>
    /// When the user can print again
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextPrintAllowedAfter = TimeSpan.Zero;

    /// <summary>
    /// How long in between each time the user can print out a task
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);
}

/// <summary>
///     Component attached to the PDA a NanoTask cartridge is inserted into for interaction handling
/// </summary>
[RegisterComponent]
public sealed partial class NanoTaskInteractionComponent : Component
{
}
