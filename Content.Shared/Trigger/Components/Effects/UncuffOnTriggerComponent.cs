using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Removes a pair of handcuffs from the entity.
/// If TargetUser is true the user will be uncuffed instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UncuffOnTriggerComponent : BaseXOnTriggerComponent;
