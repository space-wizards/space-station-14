using Content.Server.Terminator.Systems;

namespace Content.Server.Terminator.Components;

/// <summary>
/// Sets <see cref="TerminatorComponent.Target"/> after the ghost role spawns.
/// </summary>
[RegisterComponent, Access(typeof(TerminatorSystem))]
public sealed partial class TerminatorTargetComponent : Component
{
    /// <summary>
    /// The target to set after the ghost role spawns.
    /// </summary>
    [DataField("target")]
    public EntityUid? Target;
}
