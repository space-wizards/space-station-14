using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Creates a trigger when this entity is swung as a melee weapon, regardless of whether it hits something.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnMeleeSwingComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// If true, the "user" of the trigger is the first entity hit by the melee. User is null if nothing is hit.
    /// if false, user is the entity which attacked with the melee weapon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetIsUser;
}
