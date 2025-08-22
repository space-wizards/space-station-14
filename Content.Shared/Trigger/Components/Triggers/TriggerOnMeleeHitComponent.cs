using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Creates a trigger when this entity is swung as a melee weapon and hits at least one target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnMeleeHitComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// If true, this trigger will activate individually for each entity hit.
    /// If false, this trigger will always activate only once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TriggerEveryHit;

    /// <summary>
    /// If true, the "user" of the trigger is the entity hit by the melee.
    /// if false, user is the entity which attacked with the melee weapon.
    /// </summary>
    /// <remarks>If TriggerEveryHit is false, the user is randomly chosen from hit entities.</remarks>
    [DataField, AutoNetworkedField]
    public bool TargetIsUser;
}
