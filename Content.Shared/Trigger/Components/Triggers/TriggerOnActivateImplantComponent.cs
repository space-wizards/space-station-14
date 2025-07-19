using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when activating an action granted by an implant.
/// The user is the player activating it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TriggerOnActivateImplantComponent : BaseTriggerOnXComponent;
