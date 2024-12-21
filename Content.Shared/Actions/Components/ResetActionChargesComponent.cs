using Robust.Shared.GameStates;

namespace Content.Shared.Actions;

/// <summary>
/// Indicates that the entity should have its charges reset after cooldown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResetActionChargesComponent : Component
{

}
