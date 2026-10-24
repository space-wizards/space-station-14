using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Plays a light behavior on the target when this trigger is activated, of note is that the entity needs a PointLightComponent
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LightBehaviorOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The light behavior we're triggering.
    /// </summary>
    [DataField(required: true)]
    public string Behavior = string.Empty;
}
