using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Botany;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Botany
{
    public enum HarvestType : byte
    {
        NoRepeat,
        Repeat,
        SelfHarvest,
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

    public struct SeedChemQuantity
    {
        public int Min;
        public int Max;
        public int PotencyDivisor;
    }

    [Prototype("seed")]
    public class Seed : IPrototype, IIndexedPrototype
    {
        private const string SeedPrototype = "SeedBase";

        [YamlField("id")]
        public string ID { get; private set; }

        /// <summary>
        ///     Unique identifier of this seed. Do NOT set this.
        /// </summary>
        public int Uid { get; internal set; } = -1;

        #region Tracking
        [ViewVariables] [YamlField("name")] public string Name { get; set; }
        [ViewVariables] [YamlField("seedName")] public string SeedName { get; set; }

        [ViewVariables]
        [YamlField("seedNoun")]
        public string SeedNoun { get; set; } = "seeds";
        [ViewVariables] [YamlField("displayName")] public string DisplayName { get; set; }

        [ViewVariables]
        [YamlField("roundStart")]
        public bool RoundStart { get; private set; } = true;
        [ViewVariables] [YamlField("mysterious")] public bool Mysterious { get; set; }
        [ViewVariables] [YamlField("immutable")] public bool Immutable { get; set; }
        #endregion

        #region Output

        [ViewVariables]
        [YamlField("productPrototypes")]
        public List<string> ProductPrototypes { get; set; } = new();

        [ViewVariables]
        [YamlField("chemicals")]
        public Dictionary<string, SeedChemQuantity> Chemicals { get; set; } = new();
        [ViewVariables] [YamlField("consumeGasses")] public Dictionary<Gas, float> ConsumeGasses { get; set; }
        [ViewVariables] [YamlField("exudeGasses")] public Dictionary<Gas, float> ExudeGasses { get; set; }
        #endregion

        #region Tolerances

        [ViewVariables]
        [YamlField("nutrientConsumption")]
        public float NutrientConsumption { get; set; } = 0.25f;

        [ViewVariables] [YamlField("waterConsumption")] public float WaterConsumption { get; set; } = 3f;
        [ViewVariables] [YamlField("idealHeat")] public float IdealHeat { get; set; } = 293f;
        [ViewVariables] [YamlField("heatTolerance")] public float HeatTolerance { get; set; } = 20f;
        [ViewVariables] [YamlField("idealLight")] public float IdealLight { get; set; } = 7f;
        [ViewVariables] [YamlField("lightTolerance")] public float LightTolerance { get; set; } = 5f;
        [ViewVariables] [YamlField("toxinsTolerance")] public float ToxinsTolerance { get; set; } = 4f;

        [ViewVariables]
        [YamlField("lowPressureTolerance")]
        public float LowPressureTolerance { get; set; } = 25f;

        [ViewVariables]
        [YamlField("highPressureTolerance")]
        public float HighPressureTolerance { get; set; } = 200f;

        [ViewVariables]
        [YamlField("pestTolerance")]
        public float PestTolerance { get; set; } = 5f;

        [ViewVariables]
        [YamlField("weedTolerance")]
        public float WeedTolerance { get; set; } = 5f;
        #endregion

        #region General traits

        [ViewVariables]
        [YamlField("endurance")]
        public float Endurance { get; set; } = 100f;
        [ViewVariables] [YamlField("yield")] public int Yield { get; set; }
        [ViewVariables] [YamlField("lifespan")] public float Lifespan { get; set; }
        [ViewVariables] [YamlField("maturation")] public float Maturation { get; set; }
        [ViewVariables] [YamlField("production")] public float Production { get; set; }
        [ViewVariables] [YamlField("growthStages")] public int GrowthStages { get; set; } = 6;
        [ViewVariables] [YamlField("harvestRepeat")] public HarvestType HarvestRepeat { get; set; } = HarvestType.NoRepeat;

        [ViewVariables] [YamlField("potency")] public float Potency { get; set; } = 1f;
        // No, I'm not removing these.
        //public PlantSpread Spread { get; set; }
        //public PlantMutation Mutation { get; set; }
        //public float AlterTemperature { get; set; }
        //public PlantCarnivorous Carnivorous { get; set; }
        //public bool Parasite { get; set; }
        //public bool Hematophage { get; set; }
        //public bool Thorny { get; set; }
        //public bool Stinging { get; set; }
        [YamlField("ligneous")]
        public bool Ligneous { get; set; }
        // public bool Teleporting { get; set; }
        // public PlantJuicy Juicy { get; set; }
        #endregion

        #region Cosmetics
        [ViewVariables] [YamlField("plantRsi")] public ResourcePath PlantRsi { get; set; }
        [ViewVariables] [YamlField("plantIconState")] public string PlantIconState { get; set; } = "produce";
        [ViewVariables] [YamlField("bioluminescent")] public bool Bioluminescent { get; set; }
        [ViewVariables] [YamlField("bioluminescentColor")] public Color BioluminescentColor { get; set; } = Color.White;
        [ViewVariables] [YamlField("splatPrototype")] public string SplatPrototype { get; set; }
        #endregion

        public Seed Clone()
        {
            var newSeed = new Seed()
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
                SplatPrototype = SplatPrototype,
            };

            return newSeed;
        }

        public IEntity SpawnSeedPacket(EntityCoordinates transformCoordinates, IEntityManager entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var seed = entityManager.SpawnEntity(SeedPrototype, transformCoordinates);

            var seedComp = seed.EnsureComponent<SeedComponent>();
            seedComp.Seed = this;

            if (seed.TryGetComponent(out SpriteComponent sprite))
            {
                // Seed state will always be seed. Blame the spriter if that's not the case!
                sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(PlantRsi, "seed"));
            }

            seed.Name = Loc.GetString($"packet of {SeedName} {SeedNoun}");

            return seed;
        }

        private void AddToDatabase()
        {
            var plantSystem = EntitySystem.Get<PlantSystem>();
            if (plantSystem.AddSeedToDatabase(this))
            {
                Name = Uid.ToString();
            }
        }

        public IEnumerable<IEntity> AutoHarvest(EntityCoordinates position, int yieldMod = 1)
        {
            if (position.IsValid(IoCManager.Resolve<IEntityManager>()) && ProductPrototypes != null &&
                ProductPrototypes.Count > 0)
                return GenerateProduct(position, yieldMod);

            return Enumerable.Empty<IEntity>();
        }

        public IEnumerable<IEntity> Harvest(IEntity user, int yieldMod = 1)
        {
            AddToDatabase();

            if (user == null)
                return Enumerable.Empty<IEntity>();

            if (ProductPrototypes == null || ProductPrototypes.Count == 0 || Yield <= 0)
            {
                user.PopupMessageCursor(Loc.GetString("You fail to harvest anything useful."));
                return Enumerable.Empty<IEntity>();
            }

            user.PopupMessageCursor(Loc.GetString($"You harvest from the {DisplayName}"));
            return GenerateProduct(user.Transform.Coordinates, yieldMod);
        }

        public IEnumerable<IEntity> GenerateProduct(EntityCoordinates position, int yieldMod = 1)
        {
            var totalYield = 0;
            if (Yield > -1)
            {
                if (yieldMod < 0)
                {
                    yieldMod = 1;
                    totalYield = Yield;
                }
                else
                {
                    totalYield = Yield * yieldMod;
                }

                totalYield = Math.Max(1, totalYield);
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var products = new List<IEntity>();

            for (var i = 0; i < totalYield; i++)
            {
                var product = random.Pick(ProductPrototypes);

                var entity = entityManager.SpawnEntity(product, position);
                entity.RandomOffset(0.25f);
                products.Add(entity);

                var produce = entity.EnsureComponent<ProduceComponent>();

                produce.Seed = this;
                produce.Grown();

                if (Mysterious)
                {
                    entity.Name += "?";
                    entity.Description += Loc.GetString(" On second thought, something about this one looks strange.");
                }
            }

            return products;
        }

        public Seed Diverge(bool modified)
        {
            return Clone();
        }

        public bool CheckHarvest(IEntity user, IEntity held = null)
        {
            return (!Ligneous || (Ligneous && held != null && held.HasComponent<BotanySharpComponent>()));
        }
    }
}
