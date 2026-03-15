using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

// When added to an entity with BallisticAmmoProviderComponent, randomizes
// the loaded round count at MapInit to a uniform value in [MinFillFraction, MaxFillFraction]
// of the magazine's capacity. Creates per-spawn variation in partial magazines.
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomAmmoFillComponent : Component
{
    // Lower bound of the loaded-round fraction (0.0–1.0). Defaults to half-full.
    [DataField]
    public float MinFillFraction = 0.5f;

    // Upper bound of the loaded-round fraction (0.0–1.0). Defaults to completely full.
    [DataField]
    public float MaxFillFraction = 1.0f;
}
