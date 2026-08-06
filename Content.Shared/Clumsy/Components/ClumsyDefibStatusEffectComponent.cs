using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally shock itself while using a defibrillator.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClumsyDefibStatusEffectComponent : BaseClumsyStatusEffectComponent;
