using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Server.Botany;

[Prototype("seed")]
public sealed class SeedPrototype : SeedData, IPrototype
{
    [IdDataField] public string ID { get; private init; } = default!;
}

public enum HarvestType : byte
{
    NoRepeat,
    Repeat,
    SelfHarvest
}

/*
    public enum PlantSpread : byte
    {
        NoSpread,
        Creepers,
        Vines,
    }

    public enum PlantMutation : byte
    {
        NoMutation,
        Mutable,
        HighlyMutable,
    }

    public enum PlantCarnivorous : byte
    {
        NotCarnivorous,
        EatPests,
        EatLivingBeings,
    }

    public enum PlantJuicy : byte
    {
        NotJuicy,
        Juicy,
        Slippery,
    }
*/

[DataDefinition]
public partial struct SeedChemQuantity
{
    [DataField("Min")] public int Min;
    [DataField("Max")] public int Max;
    [DataField("PotencyDivisor")] public int PotencyDivisor;
}

// TODO reduce the number of friends to a reasonable level. Requires ECS-ing things like plant holder component.
[Virtual, DataDefinition]
[Access(typeof(BotanySystem), typeof(PlantHolderSystem), typeof(SeedExtractorSystem), typeof(PlantHolderComponent), typeof(ReagentEffect), typeof(MutationSystem))]
public partial class SeedData
{
    #region Tracking

    /// <summary>
    ///     The name of this seed. Determines the name of seed packets.
    /// </summary>
    [DataField("name")]
    public string Name { get; private set; } = "";

    /// <summary>
    ///     The noun for this type of seeds. E.g. for fungi this should probably be "spores" instead of "seeds". Also
    ///     used to determine the name of seed packets.
    /// </summary>
    [DataField("noun")]
    public string Noun { get; private set; } = "";

    /// <summary>
    ///     Name displayed when examining the hydroponics tray. Describes the actual plant, not the seed itself.
    /// </summary>
    [DataField("displayName")]
    public string DisplayName { get; private set; } = "";

    [DataField("mysterious")] public bool Mysterious;

    /// <summary>
    ///     If true, the properties of this seed cannot be modified.
    /// </summary>
    [DataField("immutable")] public bool Immutable;

    /// <summary>
    ///     If true, there is only a single reference to this seed and it's properties can be directly modified without
    ///     needing to clone the seed.
    /// </summary>
    [ViewVariables]
    public bool Unique = false; // seed-prototypes or yaml-defined seeds for entity prototypes will not generally be unique.
    #endregion

    #region Output
    /// <summary>
    ///     The entity prototype that is spawned when this type of seed is extracted from produce using a seed extractor.
    /// </summary>
    [DataField("packetPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PacketPrototype = "SeedBase";

    /// <summary>
    ///     The entity prototype this seed spawns when it gets harvested.
    /// </summary>
    [DataField("productPrototypes", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> ProductPrototypes = new();

    [DataField("chemicals")] public Dictionary<string, SeedChemQuantity> Chemicals = new();

    [DataField("consumeGasses")] public Dictionary<Gas, float> ConsumeGasses = new();

    [DataField("exudeGasses")] public Dictionary<Gas, float> ExudeGasses = new();

    #endregion

    #region Tolerances

    [DataField("nutrientConsumption")] public float NutrientConsumption = 0.75f;

    [DataField("waterConsumption")] public float WaterConsumption = 0.5f;
    [DataField("idealHeat")] public float IdealHeat = 293f;
    [DataField("heatTolerance")] public float HeatTolerance = 10f;
    [DataField("idealLight")] public float IdealLight = 7f;
    [DataField("lightTolerance")] public float LightTolerance = 3f;
    [DataField("toxinsTolerance")] public float ToxinsTolerance = 4f;

    [DataField("lowPressureTolerance")] public float LowPressureTolerance = 81f;

    [DataField("highPressureTolerance")] public float HighPressureTolerance = 121f;

    [DataField("pestTolerance")] public float PestTolerance = 5f;

    [DataField("weedTolerance")] public float WeedTolerance = 5f;

    [DataField("weedHighLevelThreshold")] public float WeedHighLevelThreshold = 10f;

    #endregion

    #region General traits

    [DataField("endurance")] public float Endurance = 100f;

    [DataField("yield")] public int Yield;
    [DataField("lifespan")] public float Lifespan;
    [DataField("maturation")] public float Maturation;
    [DataField("production")] public float Production;
    [DataField("growthStages")] public int GrowthStages = 6;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("harvestRepeat")] public HarvestType HarvestRepeat = HarvestType.NoRepeat;

    [DataField("potency")] public float Potency = 1f;

    /// <summary>
    ///     If true, cannot be harvested for seeds. Balances hybrids and
    ///     mutations.
    /// </summary>
    [DataField("seedless")] public bool Seedless = false;

    /// <summary>
    ///     If false, rapidly decrease health while growing. Used to kill off
    ///     plants with "bad" mutations.
    /// </summary>
    [DataField("viable")] public bool Viable = true;

    /// <summary>
    ///     If true, fruit slips players.
    /// </summary>
    [DataField("slip")] public bool Slip = false;

    /// <summary>
    ///     If true, fruits are sentient.
    /// </summary>
    [DataField("sentient")] public bool Sentient = false;

    /// <summary>
    ///     If true, a sharp tool is required to harvest this plant.
    /// </summary>
    [DataField("ligneous")] public bool Ligneous;

    // No, I'm not removing these.
    // if you re-add these, make sure that they get cloned.
    //public PlantSpread Spread { get; set; }
    //public PlantMutation Mutation { get; set; }
    //public float AlterTemperature { get; set; }
    //public PlantCarnivorous Carnivorous { get; set; }
    //public bool Parasite { get; set; }
    //public bool Hematophage { get; set; }
    //public bool Thorny { get; set; }
    //public bool Stinging { get; set; }
    // public bool Teleporting { get; set; }
    // public PlantJuicy Juicy { get; set; }

    #endregion

    #region Cosmetics

    [DataField("plantRsi", required: true)]
    public ResPath PlantRsi { get; set; } = default!;

    [DataField("plantIconState")] public string PlantIconState { get; set; } = "produce";

    [DataField("screamSound")]
    public SoundSpecifier ScreamSound = new SoundPathSpecifier("/Audio/Voice/Human/malescream_1.ogg");


    [DataField("screaming")] public bool CanScream;

    [DataField("bioluminescent")] public bool Bioluminescent;
    [DataField("bioluminescentColor")] public Color BioluminescentColor { get; set; } = Color.White;

    public float BioluminescentRadius = 2f;

    [DataField("kudzuPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))] public string KudzuPrototype = "WeakKudzu";

    [DataField("turnIntoKudzu")] public bool TurnIntoKudzu;
    [DataField("splatPrototype")] public string? SplatPrototype { get; set; }

    #endregion

    /// <summary>
    ///     The seed prototypes this seed may mutate into when prompted to.
    /// </summary>
    [DataField("mutationPrototypes", customTypeSerializer: typeof(PrototypeIdListSerializer<SeedPrototype>))]
    public List<string> MutationPrototypes = new();

    public SeedData Clone()
    {
        DebugTools.Assert(!Immutable, "There should be no need to clone an immutable seed.");

        var newSeed = new SeedData
        {
            Name = Name,
            Noun = Noun,
            DisplayName = DisplayName,
            Mysterious = Mysterious,

            PacketPrototype = PacketPrototype,
            ProductPrototypes = new List<string>(ProductPrototypes),
            MutationPrototypes = new List<string>(MutationPrototypes),
            Chemicals = new Dictionary<string, SeedChemQuantity>(Chemicals),
            ConsumeGasses = new Dictionary<Gas, float>(ConsumeGasses),
            ExudeGasses = new Dictionary<Gas, float>(ExudeGasses),

            NutrientConsumption = NutrientConsumption,
            WaterConsumption = WaterConsumption,
            IdealHeat = IdealHeat,
            HeatTolerance = HeatTolerance,
            IdealLight = IdealLight,
            LightTolerance = LightTolerance,
            ToxinsTolerance = ToxinsTolerance,
            LowPressureTolerance = LowPressureTolerance,
            HighPressureTolerance = HighPressureTolerance,
            PestTolerance = PestTolerance,
            WeedTolerance = WeedTolerance,

            Endurance = Endurance,
            Yield = Yield,
            Lifespan = Lifespan,
            Maturation = Maturation,
            Production = Production,
            GrowthStages = GrowthStages,
            HarvestRepeat = HarvestRepeat,
            Potency = Potency,

            Seedless = Seedless,
            Viable = Viable,
            Slip = Slip,
            Sentient = Sentient,
            Ligneous = Ligneous,

            PlantRsi = PlantRsi,
            PlantIconState = PlantIconState,
            Bioluminescent = Bioluminescent,
            CanScream = CanScream,
            TurnIntoKudzu = TurnIntoKudzu,
            BioluminescentColor = BioluminescentColor,
            SplatPrototype = SplatPrototype,

            // Newly cloned seed is unique. No need to unnecessarily clone if repeatedly modified.
            Unique = true,
        };

        return newSeed;
    }
}
