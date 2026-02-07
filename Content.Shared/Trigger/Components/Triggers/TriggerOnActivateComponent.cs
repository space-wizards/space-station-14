using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when activated in hand or by clicking on the entity.
/// The user is the player activating it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnActivateComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Is this interaction a complex interaction?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireComplex = true;
}
