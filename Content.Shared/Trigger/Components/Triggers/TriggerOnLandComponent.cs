using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

// Triggers when the component parent object lands.

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnLandComponent : BaseTriggerOnXComponent;
