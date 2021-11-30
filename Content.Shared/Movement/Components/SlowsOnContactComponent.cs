using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Exists just to listen to a single event. What a life.
/// </summary>
[NetworkedComponent, RegisterComponent]
public class SlowsOnContactComponent : Component
{
    public override string Name => "SlowsOnContact";
}
