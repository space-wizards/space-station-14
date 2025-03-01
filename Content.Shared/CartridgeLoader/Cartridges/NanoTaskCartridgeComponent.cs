using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Shared.CartridgeLoader.Cartridges;

/// <summary>
///     Component that indicates a PDA cartridge as containing the NanoTask program
/// </summary>
[RegisterComponent]
public sealed partial class NanoTaskCartridgeComponent : Component
{
    /// <summary>
    /// The list of tasks
    /// </summary>
    [DataField]
    public List<NanoTaskItemAndId> Tasks = new();

    /// <summary>
    /// counter for generating task IDs
    /// </summary>
    [DataField]
    public int Counter = 1;

    /// <summary>
    /// Remaining time of printing animation
    /// </summary>
    [DataField]
    public float? PrintDelayRemaining = null;

    /// <summary>
    /// How long in between each time the user can print out a task, in seconds
    /// </summary>
    [ViewVariables]
    public float PrintDelay = 5.0f;
}

/// <summary>
///     Component attached to the PDA a NanoTask cartridge is inserted into for interaction handling
/// </summary>
[RegisterComponent]
public sealed partial class NanoTaskInteractionComponent : Component
{
}
