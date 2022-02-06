using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.Atmos;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Server.Botany;

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
public struct SeedChemQuantity
{
    [DataField("Min")] public int Min;
    [DataField("Max")] public int Max;
    [DataField("PotencyDivisor")] public int PotencyDivisor;
}

[Prototype("seed")]
public sealed class SeedPrototype : IPrototype
{
    public const string Prototype = "SeedBase";

    [DataField("id", required: true)] public string ID { get; private init; } = default!;

    /// <summary>
    ///     Unique identifier of this seed. Do NOT set this.
    /// </summary>
    public int Uid { get; internal set; } = -1;

    #region Tracking

    [DataField("name")] public string Name = string.Empty;
    [DataField("seedName")] public string SeedName = string.Empty;
    [DataField("seedNoun")] public string SeedNoun = "seeds";
    [DataField("displayName")] public string DisplayName = string.Empty;

    [DataField("roundStart")] public bool RoundStart = true;
    [DataField("mysterious")] public bool Mysterious;
    [DataField("immutable")] public bool Immutable;

    #endregion

    #region Output

    [DataField("productPrototypes", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> ProductPrototypes = new();

    [DataField("chemicals")] public Dictionary<string, SeedChemQuantity> Chemicals = new();

    [DataField("consumeGasses")] public Dictionary<Gas, float> ConsumeGasses = new();

    [DataField("exudeGasses")] public Dictionary<Gas, float> ExudeGasses = new();

    #endregion

    #region Tolerances

    [DataField("nutrientConsumption")] public float NutrientConsumption = 0.25f;

    [DataField("waterConsumption")] public float WaterConsumption = 3f;
    [DataField("idealHeat")] public float IdealHeat = 293f;
    [DataField("heatTolerance")] public float HeatTolerance = 20f;
    [DataField("idealLight")] public float IdealLight = 7f;
    [DataField("lightTolerance")] public float LightTolerance = 5f;
    [DataField("toxinsTolerance")] public float ToxinsTolerance = 4f;

    [DataField("lowPressureTolerance")] public float LowPressureTolerance = 25f;

    [DataField("highPressureTolerance")] public float HighPressureTolerance = 200f;

    [DataField("pestTolerance")] public float PestTolerance = 5f;

    [DataField("weedTolerance")] public float WeedTolerance = 5f;

    #endregion

    #region General traits

    [DataField("endurance")] public float Endurance = 100f;

    [DataField("yield")] public int Yield;
    [DataField("lifespan")] public float Lifespan;
    [DataField("maturation")] public float Maturation;
    [DataField("production")] public float Production;
    [DataField("growthStages")] public int GrowthStages = 6;
    [DataField("harvestRepeat")] public HarvestType HarvestRepeat = HarvestType.NoRepeat;

    [DataField("potency")] public float Potency = 1f;

    // No, I'm not removing these.
    //public PlantSpread Spread { get; set; }
    //public PlantMutation Mutation { get; set; }
    //public float AlterTemperature { get; set; }
    //public PlantCarnivorous Carnivorous { get; set; }
    //public bool Parasite { get; set; }
    //public bool Hematophage { get; set; }
    //public bool Thorny { get; set; }
    //public bool Stinging { get; set; }

    [DataField("ligneous")] public bool Ligneous;
    // public bool Teleporting { get; set; }
    // public PlantJuicy Juicy { get; set; }

    #endregion

    #region Cosmetics

    [DataField("plantRsi", required: true)]
    public ResourcePath PlantRsi { get; set; } = default!;

    [DataField("plantIconState")] public string PlantIconState { get; set; } = "produce";

    [DataField("bioluminescent")] public bool Bioluminescent { get; set; }

    [DataField("bioluminescentColor")] public Color BioluminescentColor { get; set; } = Color.White;

    [DataField("splatPrototype")] public string? SplatPrototype { get; set; }

    #endregion

    public SeedPrototype Clone()
    {
        var newSeed = new SeedPrototype
        {
            ID = ID,
            Name = Name,
            SeedName = SeedName,
            SeedNoun = SeedNoun,
            RoundStart = RoundStart,
            Mysterious = Mysterious,

            ProductPrototypes = new List<string>(ProductPrototypes),
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

            PlantRsi = PlantRsi,
            PlantIconState = PlantIconState,
            Bioluminescent = Bioluminescent,
            BioluminescentColor = BioluminescentColor,
            SplatPrototype = SplatPrototype
        };

        return newSeed;
    }

    public SeedPrototype Diverge(bool modified)
    {
        return Clone();
    }
}
