using Content.Shared.Exterminator.Systems;

namespace Content.Shared.Exterminator.Components;

/// <summary>
/// Sets <see cref="ExterminatorComponent.Target"/> after the ghost role spawns.
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
