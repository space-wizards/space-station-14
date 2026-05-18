using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// Prevents the target from seeing while active.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class BlindnessStatusEffectComponent : Component;
