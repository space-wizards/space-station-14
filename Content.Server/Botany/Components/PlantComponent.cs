using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Access(typeof(BotanySystem), typeof(PlantSystem), typeof(PlantHolderSystem), typeof(EntityEffect))]
public sealed partial class PlantComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// How often to check for visual updates. Intentionally happens faster than growth ticks.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The last time this plant was eligible to create produce. Used to determine time between repeated harvests.
    /// </summary>
    [DataField]
    public int LastProduce;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    /// <summary>
    /// The time between growth ticks. 
    /// </summary>
    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    /// <summary>
    /// How many growth ticks this plant has been growing.
    /// </summary>
    [DataField]
    public int Age = 1;

    /// <summary>
    /// Dead plants don't do anything. They take up space until they're removed from the holder.
    /// </summary>
    [DataField]
    public bool Dead;

    /// <summary>
    /// If true, the plant will be harvested for produce upon empty-hand interaction. 
    /// </summary>
    [DataField]
    public bool Harvest;

    /// <summary>
    /// The number of ticks to skip the age checks on. Makes it take longer to get produce as this is incremented.
    /// </summary>
    [DataField]
    public int SkipAging;

    /// <summary>
    /// Indicates if this plant has has clippers used on it to make seeds.
    /// </summary>
    [DataField]
    public bool Sampled;

    /// <summary>
    /// An additional multiplier to boost severity by when doing mutation checks.
    /// </summary>
    [DataField]
    public float MutationMod = 1f;

    /// <summary>
    /// The base multiplier to use for mutation checks. Equal to the units of mutagen present in the plant when rolling.
    /// </summary>
    [DataField]
    public float MutationLevel;

    /// <summary>
    /// The plant's current health. Can't exceed the seed's Endurance
    /// </summary>
    [DataField]
    public float Health = 100;

    /// <summary>
    /// Which PlantHolder this plant is growing in. Plants are expected to have a PlantHolder with the water and nutrient they use to grow.
    /// </summary>
    [DataField]
    public EntityUid? PlantHolderUid;

    /// <summary>
    /// The seed data for this plant, that determines most of it's growth values and produce.
    /// </summary>
    [DataField]
    public SeedData? Seed;
}
