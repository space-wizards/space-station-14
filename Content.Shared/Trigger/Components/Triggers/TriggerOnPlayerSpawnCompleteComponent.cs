using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// A trigger which occurs on <see cref="PlayerSpawnCompleteEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnPlayerSpawnCompleteComponent : BaseTriggerOnXComponent;
