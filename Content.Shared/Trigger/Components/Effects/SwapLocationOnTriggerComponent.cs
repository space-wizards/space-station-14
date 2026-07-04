using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Swaps the location of the target and the user of the trigger when triggered.
/// <see cref="BaseXOnTriggerComponent.TargetUser"/> is ignored.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwapLocationOnTriggerComponent : BaseXOnTriggerComponent;
