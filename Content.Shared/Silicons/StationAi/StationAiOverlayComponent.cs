using Content.Shared.Starlight.Antags.Abductor;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Handles the static overlay for station AI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool AllowCrossGrid;

    [DataField, AutoNetworkedField]
    public float Alfa = 1f;
}
