using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will delete the entity when triggered.
/// If TargetUser is true it will delete them instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeleteOnTriggerComponent : BaseXOnTriggerComponent;
