using Robust.Shared.GameStates;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for displaying Vacuous Imposition's visuals on a player.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class CosmicImpositionFadeComponent : Component
{
    [DataField]
    public float Duration = 6f;
}
