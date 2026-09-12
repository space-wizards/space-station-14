using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to an enabled jetpack. Tracks server gas consumption timing.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedJetpackSystem))]
public sealed partial class ActiveJetpackComponent : Component
{
    [ViewVariables]
    public TimeSpan NextGasUsage = TimeSpan.Zero;
}
