using Content.Shared.Climbing;

namespace Content.Client.Movement.Components;

[RegisterComponent]

[ComponentReference(typeof(SharedClimbableComponent))]
public sealed class ClimbableComponent : SharedClimbableComponent { }
