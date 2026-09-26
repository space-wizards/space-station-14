using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Launches the owner of this component when triggered.
/// If TargetUser is true, this launches the entity that was collided with instead (because the "user" is the thing that's caused the collision, i.e. the other object).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LaunchOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// A linear impulse applied to the target, measured in kg * m / s
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Impulse = 10.0f;

    /// <summary>
    /// Set to true to speed the target up in the direction that it itself is moving.
    /// Set to false to move the target in the direction the launcher is moving.
    /// No effect if targetUser is false (the launcher will be the target)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseTargetDirection = false;
}
