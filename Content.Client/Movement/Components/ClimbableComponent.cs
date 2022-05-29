using Content.Shared.Climbing;

namespace Content.Client.Movement.Components;

[RegisterComponent]
[Friend(typeof(ClimbSystem))]
[ComponentReference(typeof(SharedClimbableComponent))]
public sealed class ClimbableComponent : SharedClimbableComponent { }
