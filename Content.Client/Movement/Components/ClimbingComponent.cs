using Content.Shared.Climbing;

namespace Content.Client.Movement.Components;

[RegisterComponent]

[ComponentReference(typeof(SharedClimbingComponent))]
public sealed class ClimbingComponent : SharedClimbingComponent { }
