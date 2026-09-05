using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components.Actions;

[NetworkedComponent, RegisterComponent]
// [AutoGenerateComponentPause]
public sealed partial class CosmicActionImpositionComponent : Component
{
    /// <summary>
    /// The duration of Vacuous Imposition's invulnerability.
    /// </summary>
    [DataField]
    public TimeSpan DurationDefault = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan DurationEmpowered = TimeSpan.FromSeconds(8);

    [DataField]
    public EntProtoId ImpositionOverlay = "EffectCosmicActionImpositionOverlay";
}
