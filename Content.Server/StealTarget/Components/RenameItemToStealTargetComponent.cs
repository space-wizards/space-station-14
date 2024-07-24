namespace Content.Shared.ImportantDocument;

/// <summary>
///     When added to an entity with a StealTargetComponent the entitys name will be
///     updated to be the name of the steal component when its spawned.
///     The entity must have a StealTargetComponent!
/// </summary>
[RegisterComponent]
public sealed partial class RenameItemToStealTargetComponent : Component
{
}
