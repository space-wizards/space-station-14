using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// Prevents blinking from occuring
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnblinkingStatusEffectComponent : Component;
