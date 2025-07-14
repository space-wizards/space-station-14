using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will explode using the entity's <see cref="ExplosiveComponent"/> when triggered.
/// TargetUser will only work of the user has ExplosiveComponent as well.
/// The User will be logged in the admin logs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExplodeOnTriggerComponent : BaseXOnTriggerComponent;
