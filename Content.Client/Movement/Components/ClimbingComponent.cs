using Content.Client.Movement.Systems;
using Content.Shared.Climbing;

namespace Content.Client.Movement.Components;

[RegisterComponent]
[Access(typeof(ClimbSystem))]
[ComponentReference(typeof(SharedClimbingComponent))]
public sealed class ClimbingComponent : SharedClimbingComponent { }
