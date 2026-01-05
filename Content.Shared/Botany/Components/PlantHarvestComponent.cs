using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Data for plant harvesting process.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(PlantHarvestSystem))]
public sealed partial class PlantHarvestComponent : Component
{
    /// <summary>
    /// Harvest repeat type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HarvestType HarvestRepeat = HarvestType.NoRepeat;

    /// <summary>
    /// Whether the plant is currently ready for harvest.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool ReadyForHarvest = false;

    /// <summary>
    /// The age of the plant when last harvested.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int LastHarvest = 0;
}

/// <summary>
/// Harvest options for plants.
/// </summary>
[Serializable, NetSerializable]
public enum HarvestType
{
    /// <summary>
    /// Plant is removed on harvest.
    /// </summary>
    NoRepeat,

    /// <summary>
    /// Plant makes produce every Production ticks.
    /// </summary>
    Repeat,

    /// <summary>
    /// Repeat, plus produce is dropped on the ground near the plant automatically.
    /// </summary>
    SelfHarvest
}
