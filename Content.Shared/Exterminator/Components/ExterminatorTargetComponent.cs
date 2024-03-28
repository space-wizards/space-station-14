using Content.Shared.Exterminator.Systems;

namespace Content.Shared.Exterminator.Components;

/// <summary>
/// Forces kill objective target after the ghost role spawns.
/// Gets transferred from the spawner to the exterminator.
/// </summary>
[RegisterComponent, Access(typeof(SharedExterminatorSystem))]
public sealed partial class ExterminatorTargetComponent : Component
{
    /// <summary>
    /// The target to set after the ghost role spawns.
    /// </summary>
    [DataField]
    public EntityUid? Target;
}
