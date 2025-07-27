using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
///     This is used for marking step trigger events that require the entity alive or crit.
/// </summary>
/// <remarks>
///     Works only with <see cref="StepTriggerComponent"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class StepTriggerOnAliveComponent : Component;
