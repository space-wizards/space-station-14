using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the component parent object lands.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnLandComponent : BaseTriggerOnXComponent;
