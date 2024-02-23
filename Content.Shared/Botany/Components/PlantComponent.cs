using Content.Shared.Botany.Systems;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for all plant entities regardless of what they do.
/// Handles growth stages.
/// </summary>
[RegisterComponent, Access(typeof(PlantSystem))]
public sealed partial class PlantComponent : Component
{
    /// <summary>
    /// How many produce entities this plant yields.
    /// </summary>
    [DataField(required: true)]
    public int Yield;

    [DataField(required: true)]
    public int Lifespan;

    [DataField(required: true)]
    public int Maturation;

    [DataField(required: true)]
    public int Production;

    [DataField]
    public HarvestType HarvestType;
}

public enum HarvestType : byte
{
    NoRepeat,
    Repeat,
    SelfHarvest
}

/// <summary>
/// Raised on the plant entity when it gets harvested.
/// If user is null it was self-harvested.
/// </summary>
[ByRefEvent]
public record struct PlantHarvestedEvent(EntityUid? User = null);

/// <summary>
/// Raised on a plant entity when creating a new seed plant from it.
/// Traits must be copied across here.
/// </summary>
[ByRefEvent]
public record struct PlantCopyTraitsEvent(EntityUid Plant);
