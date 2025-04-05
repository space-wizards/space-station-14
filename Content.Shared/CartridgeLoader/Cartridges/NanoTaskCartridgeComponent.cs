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
    public List<NanoTaskItemAndDepartment> Tasks = [];

    public EntityUid? ActorUid = null;
}

/// <summary>
///     Component attached to the PDA a NanoTask cartridge is inserted into for interaction handling
/// </summary>
[RegisterComponent]
public sealed partial class NanoTaskInteractionComponent : Component
{
}
