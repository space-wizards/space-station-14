using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when this entity is swung as a melee weapon and hits nothing.
/// The user is the entity swinging the weapon.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnMeleeMissComponent : BaseTriggerOnXComponent;
