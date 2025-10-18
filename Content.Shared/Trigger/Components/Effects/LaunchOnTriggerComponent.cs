using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Launches the owner of this component when triggered.
/// If TargetUser is true, this launches the entity that was collided with instead (because the "user" is the thing that's caused the collision, i.e. the other object).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LaunchOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField, AutoNetworkedField]
    public float Speed = 10.0f;
}
