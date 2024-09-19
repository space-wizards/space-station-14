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

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public int LastProduce;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    [DataField]
    public int Age = 1;

    [DataField]
    public bool Dead;

    [DataField]
    public bool Harvest;

    [DataField]
    public int SkipAging;

    [DataField]
    public bool Sampled;

    [DataField]
    public float MutationMod = 1f;

    [DataField]
    public float MutationLevel;

    [DataField]
    public float Health = 100;

    [DataField]
    public EntityUid PlantHolderUid = EntityUid.Invalid;

    [DataField]
    public SeedData? Seed;
}
