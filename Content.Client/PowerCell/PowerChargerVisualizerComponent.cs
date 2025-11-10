using Content.Shared.Power.Components;

namespace Content.Client.PowerCell;

[RegisterComponent]
[Access(typeof(PowerChargerVisualizerSystem))]
public sealed partial class PowerChargerVisualsComponent : Component
{
    /// <summary>
    /// The base sprite state used if the power cell charger does not contain a power cell.
    /// </summary>
    [DataField]
    public string EmptyState = "empty";

    /// <summary>
    /// The base sprite state used if the power cell charger contains a power cell.
    /// </summary>
    [DataField]
    public string OccupiedState = "full";

    /// <summary>
    /// A mapping of the indicator light overlays for the power cell charger.
    /// <see cref="CellChargerStatus.Off"/> Maps to the state used when the charger is out of power/disabled.
    /// <see cref="CellChargerStatus.Empty"/> Maps to the state used when the charger does not contain a power cell.
    /// <see cref="CellChargerStatus.Charging"/> Maps to the state used when the charger is charging a power cell.
    /// <see cref="CellChargerStatus.Charged"/> Maps to the state used when the charger contains a fully charged power cell.
    /// </summary>
    [DataField]
    public Dictionary<CellChargerStatus, string> LightStates = new()
    {
        [CellChargerStatus.Off] = "light-off",
        [CellChargerStatus.Empty] = "light-empty",
        [CellChargerStatus.Charging] = "light-charging",
        [CellChargerStatus.Charged] = "light-charged",
    };
}
