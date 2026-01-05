using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Data for weed and pest problems which can happen to plants - how well plant tolerates them,
/// chances to develop them, how big of a problem they will be.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(WeedPestGrowthSystem))]
public sealed partial class WeedPestGrowthComponent : Component
{
    /// <summary>
    /// Maximum weed level the plant can tolerate before taking damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedTolerance = 5f;

    /// <summary>
    /// Amount of damage dealt to the plant per successful weed damage tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedDamageAmount = 1f;

    /// <summary>
    /// Maximum pest level the plant can tolerate before taking damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PestTolerance = 5f;

    /// <summary>
    /// Chance per tick for pests to grow around this plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PestGrowthChance = 0.01f;

    /// <summary>
    /// Amount of pest growth per successful pest growth tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PestGrowthAmount = 0.5f;

    /// <summary>
    /// Amount of damage dealt to the plant per successful pest damage tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PestDamageAmount = 1f;
}
