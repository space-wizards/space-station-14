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
    /// Trigger only when this fixture collides.
    /// If null, trigger when this entity collides.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? FixtureID;

    /// <summary>
    /// Doesn't trigger if the other colliding fixture is nonhard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreOtherNonHard = true;

    /// <summary>
    /// If not null, limits the amount of times this component can trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? MaxTriggers;
}
