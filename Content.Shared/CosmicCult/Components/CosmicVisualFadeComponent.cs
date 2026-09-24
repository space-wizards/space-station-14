using Robust.Shared.GameStates;

namespace Content.Shared.CosmicCult.Components;

// This should probably be handled through some genericized animation systems and libraries
// But we don't have those
// And it's not within scope or sane timeframe to assemble such just for the two use-cases cosmic cult needs
// Suffering.

/// <summary>
/// Component for handling animated fading visuals on various Cosmic Cult VFX.
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class CosmicVisualFadeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Duration = 6f;
}
