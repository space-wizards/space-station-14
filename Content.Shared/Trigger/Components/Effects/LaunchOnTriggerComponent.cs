using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Launches the owner on trigger.
/// If TargetUser is true, launches the target instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LaunchOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public float Speed;
}
