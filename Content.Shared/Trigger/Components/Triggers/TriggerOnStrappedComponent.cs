using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the component parent is strapped.
/// This is intended to be used on objects like chairs or beds.
/// The parent object should be the object "containing" the strap so to speak.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStrappedComponent : BaseTriggerOnXComponent;
