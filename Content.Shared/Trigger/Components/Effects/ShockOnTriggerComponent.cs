using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will electrocute the entity when triggered.
/// If TargetUser is true it will electrocute the user instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShockOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Electrocute entity containing this entity instead (for example for wearable clothing).
    /// Has priority over TargetUser.
    /// </summary>
    /// <remarks>
    /// TODO: Make this more generic so it can be used for all triggers.
    /// Maybe a BeforeTriggerEvent where we modify the target.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool TargetContainer;

    /// <summary>
    /// The force of an electric shock when the trigger is triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Damage = 5;

    /// <summary>
    /// Duration of electric shock when the trigger is triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);
}
