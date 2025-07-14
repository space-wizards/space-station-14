using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when attempting to shoot a gun while it's empty.
/// The user is the player holding the gun.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TriggerOnGunEmptyComponent : BaseTriggerOnXComponent;
