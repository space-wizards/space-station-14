using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when colliding with another entity.
/// The user is the entity collided with.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnCollideComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The fixture with which to collide.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string FixtureID = string.Empty;

    /// <summary>
    /// Doesn't trigger if the other colliding fixture is nonhard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreOtherNonHard = true;
}
