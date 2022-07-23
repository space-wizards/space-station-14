using Content.Shared.Climbing;

namespace Content.Server.Climbing.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedClimbableComponent))]
public sealed class ClimbableComponent : SharedClimbableComponent
{
    /// <summary>
    ///     The time it takes to climb onto the entity.
    /// </summary>
    [ViewVariables]
    [DataField("delay")]
    public float ClimbDelay = 0.8f;
}
