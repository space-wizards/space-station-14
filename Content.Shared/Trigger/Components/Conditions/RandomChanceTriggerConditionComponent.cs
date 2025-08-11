using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// This condition will cancel triggers based on random chance.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomChanceTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField, AutoNetworkedField]
    public float FailureChance = .1f;
}
