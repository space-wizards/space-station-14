using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// A trigger which occurs on <see cref="PlayerSpawnCompleteEvent"/>.
/// </summary>
/// <remarks>This does not work with <see cref="TraitSystem"/>, as it would add this component while the event is getting raised.</remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnPlayerSpawnCompleteComponent : BaseTriggerOnXComponent;
