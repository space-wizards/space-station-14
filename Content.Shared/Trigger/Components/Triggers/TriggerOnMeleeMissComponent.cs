using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Creates a trigger when this entity is swung as a melee weapon and hits nothing.
/// User is the entity swinging the weapon.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnMeleeMissComponent : BaseTriggerOnXComponent;
