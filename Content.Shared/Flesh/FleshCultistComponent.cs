using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Flesh;

[RegisterComponent, NetworkedComponent]
public sealed class FleshCultistComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public FixedPoint2 Hunger = 140;

    [ViewVariables(VVAccess.ReadWrite), DataField("hungerСonsumption")]
    public FixedPoint2 HungerСonsumption = -0.07; // 80 hunger in 30 minutes

    [ViewVariables(VVAccess.ReadWrite), DataField("maxHunger")]
    public FixedPoint2 MaxHunger = 200;

    [ViewVariables(VVAccess.ReadWrite), DataField("startingEvolutionPoint")]
    public FixedPoint2 StartingEvolutionPoint = 30;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("bulletAcidSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BulletAcidSpawnId = "BulletSplashAcid";

    [ViewVariables(VVAccess.ReadWrite), DataField("speciesWhitelist")]
    public List<string> SpeciesWhitelist = new()
    {
        "Human",
        "Reptilian",
        "Dwarf",
    };

    [DataField("adrenalinReagents")] public List<Solution.ReagentQuantity> AdrenalinReagents = new()
    {
        new Solution.ReagentQuantity("Ephedrine", 10),
    };

    [DataField("healDevourReagents")] public List<Solution.ReagentQuantity> HealDevourReagents = new()
    {
        new Solution.ReagentQuantity("Omnizine", 15),
        new Solution.ReagentQuantity("DexalinPlus", 5),
        new Solution.ReagentQuantity("Iron", 5)
    };

    [DataField("healBloodAbsorbReagents")] public List<Solution.ReagentQuantity> HealBloodAbsorbReagents = new()
    {
        new Solution.ReagentQuantity("Omnizine", 1),
        new Solution.ReagentQuantity("DexalinPlus", 0.50),
        new Solution.ReagentQuantity("Iron", 0.50)
    };

    [DataField("bloodAbsorbSound")]
    public SoundSpecifier BloodAbsorbSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField("devourTime")] public float DevourTime = 10f;

    [DataField("devourSound")]
    public SoundSpecifier DevourSound = new SoundPathSpecifier("/Audio/Animals/Flesh/devour_flesh_cultist.ogg");

    [DataField("stolenCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenCurrencyPrototype = "StolenMutationPoint";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("fleshBladeSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BladeSpawnId = "FleshBlade";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("fleshFistSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FistSpawnId = "FleshFist";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("clawSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ClawSpawnId = "FleshClaw";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("spikeHandGunSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpikeHandGunSpawnId = "FleshSpikeHandGun";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("armorSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ArmorSpawnId = "ClothingOuterArmorFlesh";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("spiderLegsSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpiderLegsSpawnId = "ClothingFleshSpiderLegs";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("fleshMutationMobId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FleshMutationMobId = "MobFleshAbom";

    [ViewVariables(VVAccess.ReadWrite), DataField("soundMutation")]
    public SoundSpecifier SoundMutation = new SoundPathSpecifier("/Audio/Animals/Flesh/flesh_cultist_mutation.ogg");

    [DataField("fleshHeartId", customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>)),
     ViewVariables(VVAccess.ReadWrite)]
    public string FleshHeartId = "FleshHeart";

    [ViewVariables(VVAccess.ReadWrite), DataField("soundThrowWorm")]
    public SoundSpecifier? SoundThrowWorm = new SoundPathSpecifier("/Audio/Animals/Flesh/throw_worm.ogg");

    [ViewVariables(VVAccess.ReadWrite),
     DataField("wormMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WormMobSpawnId = "MobFleshWorm";

    [ViewVariables] public float Accumulator = 0;

    [ViewVariables] public float AccumulatorStarveNotify = 0;

}
