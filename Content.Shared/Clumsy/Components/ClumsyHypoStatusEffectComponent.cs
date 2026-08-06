using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally use hyposprays on themselves instead of their target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClumsyHypoStatusEffectComponent : BaseClumsyStatusEffectComponent;
