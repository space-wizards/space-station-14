using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

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
    public float PrintDelayRemaining = float.NaN;

    /// <summary>
    /// How long in between each time the user can print out a task, in seconds
    /// </summary>
    [ViewVariables]
    public float PrintDelay = 5.0f;
}

[RegisterComponent]
public sealed partial class NanoTaskInteractionComponent : Component
{
}
