using Content.Shared.Tools.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity with <see cref="SimpleToolUsageComponent"/> when the correct tool
/// is used on it and the DoAfter has finished.
/// The user is the player using the tool.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnSimpleToolUsageComponent : BaseTriggerOnXComponent;
