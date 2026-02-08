using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Sends a trigger when the entity starts to rot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnRotComponent : BaseTriggerOnXComponent;
