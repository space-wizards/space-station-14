using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the owner of this component is ingested, like a pill or food.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnIngestedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Whether the fed entity is the user.
    /// If false, the entity feeding will be the user.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EatingIsUser = true;
}
