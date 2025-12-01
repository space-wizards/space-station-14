using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will explode using the entity's <see cref="ExplosiveComponent"/> when triggered.
/// TargetUser will only work of the user has ExplosiveComponent as well.
/// The User will be logged in the admin logs.
/// </summary>
/// <summary>
/// TODO: Allow this to work without an ExplosiveComponent on the user via QueueExplosion.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplodeOnTriggerComponent : BaseXOnTriggerComponent;
