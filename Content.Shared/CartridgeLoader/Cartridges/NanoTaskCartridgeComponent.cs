using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader.Cartridges;

/// <summary>
///     Component that indicates a PDA cartridge as containing the NanoTask program
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class NanoTaskCartridgeComponent : Component
{
    /// <summary>
    /// The list of tasks
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<NanoTaskItemAndId> Tasks = new();

    /// <summary>
    /// counter for generating task IDs
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Counter = 1;

    /// <summary>
    /// When the user can print again
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPrintAllowedAfter = TimeSpan.Zero;

    /// <summary>
    /// How long in between each time the user can print out a task
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);
}
