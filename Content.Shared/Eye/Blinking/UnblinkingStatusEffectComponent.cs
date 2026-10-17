using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// Adds independent timing offsets to automatic blinking.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnblinkingStatusEffectComponent : Component;
