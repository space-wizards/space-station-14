using Content.Shared.Climbing;

namespace Content.Client.Movement.Components;

[RegisterComponent]
[Friend(typeof(ClimbSystem))]
[ComponentReference(typeof(SharedClimbingComponent))]
public sealed class ClimbingComponent : SharedClimbingComponent { }
